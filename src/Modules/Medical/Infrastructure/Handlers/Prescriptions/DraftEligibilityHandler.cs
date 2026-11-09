using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

/// <summary>
/// Checks the patient's entitlement for a draft under the authenticated doctor's own provider login and records
/// the result on the draft. The doctor and clinic come from the authenticated scope, never from the request.
/// </summary>
public sealed class CheckDraftEligibilityHandler(
    PrescriptionDbContext dbContext,
    IInsuranceProviderRegistry providerRegistry,
    IRequestContext requestContext)
    : IRequestHandler<CheckDraftEligibilityCommand, CheckDraftEligibilityResult>
{
    public async Task<CheckDraftEligibilityResult> Handle(CheckDraftEligibilityCommand request, CancellationToken cancellationToken)
    {
        if (!CredentialRequestScope.TryResolve(requestContext, out Guid doctorId, out Guid clinicId))
        {
            return Failure("دامنه دسترسی پزشک و درمانگاه مشخص نیست.");
        }

        PrescriptionRecord? prescription = await dbContext.Prescriptions
            .FirstOrDefaultAsync(
                item => item.Id == request.PrescriptionId && item.DoctorId == doctorId && item.ClinicId == clinicId,
                cancellationToken);
        if (prescription is null)
        {
            return Failure("نسخه یافت نشد.");
        }

        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection))
        {
            return Failure("بررسی حق بیمه فقط برای نسخه‌های پیش‌نویس امکان‌پذیر است.");
        }

        if (string.IsNullOrWhiteSpace(prescription.PatientNationalId))
        {
            return Failure("کد ملی بیمار برای بررسی حق بیمه ثبت نشده است.");
        }

        if (string.IsNullOrWhiteSpace(prescription.SelectedProviderKey) ||
            providerRegistry.GetAdapter(prescription.SelectedProviderKey) is not { } adapter)
        {
            return Failure("بیمه‌گر مورد نظر انتخاب نشده است.");
        }

        AdapterResult<EntitlementResponse> result = await adapter.CheckEntitlementAsync(
            new EntitlementRequest
            {
                PatientNationalId = prescription.PatientNationalId,
                DoctorId = doctorId,
                ClinicId = clinicId
            },
            cancellationToken);
        if (result.IsNotSupported)
        {
            return Failure("بیمه‌گر از استعلام حق بیمه پشتیبانی نمی‌کند.");
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return Failure(result.Error?.UserMessage ?? "خطا در استعلام حق بیمه.");
        }

        prescription.LastEligibilityStatus = result.Data.Status;
        prescription.LastEligibilityCheckedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new CheckDraftEligibilityResult
        {
            IsSuccess = true,
            Status = result.Data.Status,
            TrackingCode = result.Data.TrackingCode
        };
    }

    private static CheckDraftEligibilityResult Failure(string message) =>
        new() { IsSuccess = false, Status = EligibilityStatus.Unknown, ErrorMessage = message };
}
