using FluentAssertions;
using Moq;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Medical.Infrastructure.Handlers.Prescriptions;
using Vitarish.Requests.Application.Abstractions;

namespace Medical.Tests.Infrastructure.Handlers.Prescriptions;

public sealed class DrugConsumptionOptionsHandlerTests
{
    private const string ProviderKey = "PROVIDER";
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();

    [Fact]
    public async Task Provider_without_the_capability_needs_no_consumption_values()
    {
        Mock<IInsuranceProviderAdapter> adapter = new();
        GetDrugConsumptionOptionsHandler handler = CreateHandler(adapter.Object, authenticated: true);

        DrugConsumptionOptionsResultDto result = await handler.Handle(Query(), CancellationToken.None);

        result.IsSupported.Should().BeFalse();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Caller_without_doctor_and_clinic_scope_never_reaches_the_provider()
    {
        Mock<IInsuranceProviderAdapter> adapter = new();
        Mock<IInsuranceProviderDrugConsumptionAdapter> capability = adapter.As<IInsuranceProviderDrugConsumptionAdapter>();
        GetDrugConsumptionOptionsHandler handler = CreateHandler(adapter.Object, authenticated: false);

        DrugConsumptionOptionsResultDto result = await handler.Handle(Query(), CancellationToken.None);

        result.IsSupported.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        capability.Verify(
            value => value.GetDrugConsumptionOptionsAsync(It.IsAny<DrugConsumptionOptionsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Provider_login_is_resolved_from_the_authenticated_scope_and_the_options_are_mapped()
    {
        DrugConsumptionOptionsRequest? sent = null;
        Mock<IInsuranceProviderAdapter> adapter = new();
        _ = adapter.As<IInsuranceProviderDrugConsumptionAdapter>()
            .Setup(value => value.GetDrugConsumptionOptionsAsync(It.IsAny<DrugConsumptionOptionsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DrugConsumptionOptionsRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(AdapterResult<DrugConsumptionOptions>.Success(new DrugConsumptionOptions
            {
                Units = [new DrugConsumptionUnit("24", "Each", null, [new DrugConsumptionOption("580", "1/4", null)])],
                Instructions = [new DrugConsumptionOption("2546009", "Night", null)],
                Usages = [new DrugConsumptionOption("231271000", "Intra-articular", null)],
                Methods = []
            }));
        GetDrugConsumptionOptionsHandler handler = CreateHandler(adapter.Object, authenticated: true);

        DrugConsumptionOptionsResultDto result = await handler.Handle(Query(), CancellationToken.None);

        sent.Should().NotBeNull();
        sent!.DoctorId.Should().Be(_doctorId);
        sent.ClinicId.Should().Be(_clinicId);
        sent.FormCode.Should().Be("17");
        result.IsSuccess.Should().BeTrue();
        result.Units.Single().Amounts.Single().Id.Should().Be("580");
        result.Instructions.Single().Id.Should().Be("2546009");
        result.Usages.Single().Id.Should().Be("231271000");
    }

    private GetDrugConsumptionOptionsHandler CreateHandler(IInsuranceProviderAdapter adapter, bool authenticated)
    {
        Mock<IInsuranceProviderRegistry> registry = new();
        _ = registry.Setup(value => value.GetAdapter(ProviderKey)).Returns(adapter);
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.OwnerId).Returns(authenticated ? _doctorId.ToString() : null);
        _ = context.SetupGet(value => value.TenantId).Returns(authenticated ? _clinicId.ToString() : null);
        return new GetDrugConsumptionOptionsHandler(registry.Object, context.Object);
    }

    private static GetDrugConsumptionOptionsQuery Query() => new() { ProviderKey = ProviderKey, FormCode = "17" };
}
