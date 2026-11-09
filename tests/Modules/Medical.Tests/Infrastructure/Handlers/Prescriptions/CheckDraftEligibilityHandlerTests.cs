using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Infrastructure.Data;
using Vitarish.Medical.Infrastructure.Handlers.Prescriptions;
using Vitarish.Requests.Application.Abstractions;

namespace Medical.Tests.Infrastructure.Handlers.Prescriptions;

public sealed class CheckDraftEligibilityHandlerTests
{
    private const string ProviderKey = "PROVIDER";
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();
    private readonly string _databaseName = $"draft-eligibility-{Guid.NewGuid():N}";

    [Fact]
    public async Task Eligible_answer_is_recorded_on_the_callers_own_draft()
    {
        Mock<IInsuranceProviderAdapter> adapter = EligibleAdapter(out Mock<IInsuranceProviderAdapter> mock);
        PrescriptionRecord draft = await AddDraftAsync(_doctorId, _clinicId);
        await using PrescriptionDbContext database = CreateDatabase(_clinicId);

        CheckDraftEligibilityResult result = await CreateHandler(database, adapter.Object).Handle(
            new CheckDraftEligibilityCommand { PrescriptionId = draft.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(EligibilityStatus.Eligible);
        (await LastStatusAsync(_clinicId)).Should().Be(EligibilityStatus.Eligible);
        mock.Verify(value => value.CheckEntitlementAsync(
            It.Is<EntitlementRequest>(request => request.DoctorId == _doctorId && request.ClinicId == _clinicId && request.PatientNationalId == "1111111111"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Another_doctors_or_clinics_draft_is_not_found_and_the_provider_is_never_asked(bool otherDoctor, bool otherClinic)
    {
        Mock<IInsuranceProviderAdapter> adapter = EligibleAdapter(out Mock<IInsuranceProviderAdapter> mock);
        Guid draftClinicId = otherClinic ? Guid.NewGuid() : _clinicId;
        PrescriptionRecord draft = await AddDraftAsync(otherDoctor ? Guid.NewGuid() : _doctorId, draftClinicId);
        await using PrescriptionDbContext database = CreateDatabase(_clinicId);

        CheckDraftEligibilityResult result = await CreateHandler(database, adapter.Object).Handle(
            new CheckDraftEligibilityCommand { PrescriptionId = draft.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await LastStatusAsync(draftClinicId)).Should().BeNull();
        mock.Verify(value => value.CheckEntitlementAsync(It.IsAny<EntitlementRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Caller_without_doctor_and_clinic_scope_is_refused()
    {
        Mock<IInsuranceProviderAdapter> adapter = EligibleAdapter(out Mock<IInsuranceProviderAdapter> mock);
        PrescriptionRecord draft = await AddDraftAsync(_doctorId, _clinicId);
        await using PrescriptionDbContext database = CreateDatabase(_clinicId);
        Mock<IRequestContext> anonymous = new();

        CheckDraftEligibilityResult result = await new CheckDraftEligibilityHandler(database, Registry(adapter.Object), anonymous.Object)
            .Handle(new CheckDraftEligibilityCommand { PrescriptionId = draft.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        mock.Verify(value => value.CheckEntitlementAsync(It.IsAny<EntitlementRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Provider_failure_leaves_the_draft_without_a_recorded_answer()
    {
        Mock<IInsuranceProviderAdapter> adapter = new();
        _ = adapter
            .Setup(value => value.CheckEntitlementAsync(It.IsAny<EntitlementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdapterResult<EntitlementResponse>.NotSupported());
        PrescriptionRecord draft = await AddDraftAsync(_doctorId, _clinicId);
        await using PrescriptionDbContext database = CreateDatabase(_clinicId);

        CheckDraftEligibilityResult result = await CreateHandler(database, adapter.Object).Handle(
            new CheckDraftEligibilityCommand { PrescriptionId = draft.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        (await LastStatusAsync(_clinicId)).Should().BeNull();
    }

    private Mock<IInsuranceProviderAdapter> EligibleAdapter(out Mock<IInsuranceProviderAdapter> mock)
    {
        mock = new Mock<IInsuranceProviderAdapter>();
        _ = mock
            .Setup(value => value.CheckEntitlementAsync(It.IsAny<EntitlementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdapterResult<EntitlementResponse>.Success(new EntitlementResponse { Status = EligibilityStatus.Eligible }));
        return mock;
    }

    private CheckDraftEligibilityHandler CreateHandler(PrescriptionDbContext database, IInsuranceProviderAdapter adapter)
    {
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.OwnerId).Returns(_doctorId.ToString());
        _ = context.SetupGet(value => value.TenantId).Returns(_clinicId.ToString());
        return new CheckDraftEligibilityHandler(database, Registry(adapter), context.Object);
    }

    private static IInsuranceProviderRegistry Registry(IInsuranceProviderAdapter adapter)
    {
        Mock<IInsuranceProviderRegistry> registry = new();
        _ = registry.Setup(value => value.GetAdapter(ProviderKey)).Returns(adapter);
        return registry.Object;
    }

    /// <summary>A context for one clinic over the shared in-memory store; the tenant filter hides other clinics' rows.</summary>
    private PrescriptionDbContext CreateDatabase(Guid tenantClinicId)
    {
        DbContextOptions<PrescriptionDbContext> options = new DbContextOptionsBuilder<PrescriptionDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        Mock<IRequestContext> context = new();
        _ = context.SetupGet(value => value.TenantId).Returns(tenantClinicId.ToString());
        return new PrescriptionDbContext(options, context.Object, Mock.Of<IHttpContextAccessor>(), []);
    }

    private async Task<EligibilityStatus?> LastStatusAsync(Guid clinicId)
    {
        await using PrescriptionDbContext database = CreateDatabase(clinicId);
        return (await database.Prescriptions.SingleAsync()).LastEligibilityStatus;
    }

    private async Task<PrescriptionRecord> AddDraftAsync(Guid doctorId, Guid clinicId)
    {
        await using PrescriptionDbContext database = CreateDatabase(clinicId);
        PrescriptionRecord draft = new()
        {
            Id = Guid.NewGuid(),
            TenantId = clinicId.ToString(),
            DoctorId = doctorId,
            ClinicId = clinicId,
            PatientId = Guid.NewGuid(),
            PatientNationalId = "1111111111",
            SelectedProviderKey = ProviderKey,
            Status = PrescriptionStatus.Draft
        };
        _ = database.Prescriptions.Add(draft);
        _ = await database.SaveChangesAsync();
        return draft;
    }
}
