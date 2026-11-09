using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions;
using Vitarish.Modules.Base.Core.Platform.Transactions;

namespace Medical.Tests.Infrastructure.Providers.Prescriptions;

/// <summary>
/// The registry hands out the decorator, so optional provider capabilities are reachable only if the
/// decorator forwards them to the wrapped adapter.
/// </summary>
public sealed class ResilientAdapterDecoratorCapabilityTests
{
    [Fact]
    public async Task Authorization_reaches_an_inner_adapter_that_supports_it()
    {
        Uri login = new("https://provider.example/authorize");
        Mock<IInsuranceProviderAdapter> inner = new();
        Mock<IInsuranceProviderAuthorizationAdapter> authorization = inner.As<IInsuranceProviderAuthorizationAdapter>();
        _ = authorization
            .Setup(value => value.BeginAuthorizationAsync(It.IsAny<ProviderAuthorizationStartRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdapterResult<ProviderAuthorizationStartResponse>.Success(new ProviderAuthorizationStartResponse { AuthorizationUri = login }));
        _ = authorization
            .Setup(value => value.CompleteAuthorizationAsync(It.IsAny<ProviderAuthorizationCompleteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdapterResult.Success());
        IInsuranceProviderAuthorizationAdapter decorator = CreateDecorator(inner.Object);

        AdapterResult<ProviderAuthorizationStartResponse> started = await decorator.BeginAuthorizationAsync(
            new ProviderAuthorizationStartRequest { CredentialId = Guid.NewGuid() });
        AdapterResult completed = await decorator.CompleteAuthorizationAsync(
            new ProviderAuthorizationCompleteRequest { CredentialId = Guid.NewGuid(), AuthorizationCode = "code", State = "state" });

        started.Data!.AuthorizationUri.Should().Be(login);
        completed.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Authorization_is_not_supported_when_the_inner_adapter_lacks_it()
    {
        IInsuranceProviderAuthorizationAdapter decorator = CreateDecorator(new Mock<IInsuranceProviderAdapter>().Object);

        (await decorator.BeginAuthorizationAsync(new ProviderAuthorizationStartRequest { CredentialId = Guid.NewGuid() }))
            .IsNotSupported.Should().BeTrue();
        (await decorator.CompleteAuthorizationAsync(
            new ProviderAuthorizationCompleteRequest { CredentialId = Guid.NewGuid(), AuthorizationCode = "code", State = "state" }))
            .IsNotSupported.Should().BeTrue();
    }

    [Fact]
    public async Task Drug_consumption_options_reach_an_inner_adapter_that_supports_them()
    {
        Mock<IInsuranceProviderAdapter> inner = new();
        _ = inner.Setup(value => value.ProviderKey).Returns("PROVIDER");
        _ = inner.As<IInsuranceProviderDrugConsumptionAdapter>()
            .Setup(value => value.GetDrugConsumptionOptionsAsync(It.IsAny<DrugConsumptionOptionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdapterResult<DrugConsumptionOptions>.Success(new DrugConsumptionOptions
            {
                Units = [],
                Instructions = [new DrugConsumptionOption("1", "Night", null)],
                Usages = [],
                Methods = []
            }));
        IInsuranceProviderDrugConsumptionAdapter decorator = CreateDecorator(inner.Object);

        AdapterResult<DrugConsumptionOptions> result = await decorator.GetDrugConsumptionOptionsAsync(
            new DrugConsumptionOptionsRequest { DoctorId = Guid.NewGuid(), ClinicId = Guid.NewGuid(), FormCode = "17" });

        result.Data!.Instructions.Single().Id.Should().Be("1");
    }

    [Fact]
    public async Task Drug_consumption_options_are_not_supported_when_the_inner_adapter_lacks_them()
    {
        IInsuranceProviderDrugConsumptionAdapter decorator = CreateDecorator(new Mock<IInsuranceProviderAdapter>().Object);

        AdapterResult<DrugConsumptionOptions> result = await decorator.GetDrugConsumptionOptionsAsync(
            new DrugConsumptionOptionsRequest { DoctorId = Guid.NewGuid(), ClinicId = Guid.NewGuid(), FormCode = "17" });

        result.IsNotSupported.Should().BeTrue();
    }

    private static ResilientAdapterDecorator CreateDecorator(IInsuranceProviderAdapter inner)
    {
        Mock<IProviderResiliencePipelineProvider> resilience = new();
        _ = resilience.Setup(value => value.GetOptions(It.IsAny<string>())).Returns(ProviderResilienceOptions.Default);
        Mock<IExternalApiTransactionLogger> transactions = new();
        _ = transactions
            .Setup(value => value.BeginTransactionAsync(It.IsAny<ApiTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        return new ResilientAdapterDecorator(
            inner,
            Mock.Of<IProviderErrorMapper>(),
            resilience.Object,
            transactions.Object,
            NullLogger<ResilientAdapterDecorator>.Instance);
    }
}
