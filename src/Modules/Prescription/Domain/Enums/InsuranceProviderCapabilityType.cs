namespace Vitarish.Prescription.Domain.Enums;

/// <summary>
/// Capabilities that an insurance provider may support.
/// Used to gate UI actions and orchestration operations.
/// </summary>
public enum InsuranceProviderCapabilityType
{
    SupportsIdentityVerification,
    SupportsEntitlementCheck,
    SupportsVisitPrescription,
    SupportsDrugPrescription,
    SupportsParaclinicPrescription,
    SupportsMedicalServicePrescription,
    SupportsReferralPrescription,
    SupportsPhysiotherapyPrescription,
    SupportsDentalPrescription,
    SupportsFetchPrescription,
    SupportsEditPrescription,
    SupportsCancelPrescription,
    SupportsReferenceDrugSync,
    SupportsReferenceServiceSync,
    SupportsPriceInquiry,
    SupportsAllowedCountInquiry,
    SupportsWarningInquiry,
    SupportsProviderSession,
    SupportsOtpAuthentication,
    SupportsDigitalSignature,
    SupportsDitasSubmission,
    SupportsPaperFallbackRegistration
}
