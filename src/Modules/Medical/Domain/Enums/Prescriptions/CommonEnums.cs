namespace Vitarish.Medical.Domain.Enums.Prescriptions;

/// <summary>
/// Status of a doctor's credential with an insurance provider.
/// </summary>
public enum CredentialStatus
{
    Active,
    Expired,
    Revoked,
    Pending
}

/// <summary>
/// Type of prescription item.
/// </summary>
public enum PrescriptionItemCategory
{
    Visit,
    Drug,
    Lab,
    Imaging,
    Paraclinic,
    MedicalService,
    Physiotherapy,
    Dental,
    Referral,
    MedicalEquipment
}

/// <summary>
/// Eligibility inquiry result status.
/// </summary>
public enum EligibilityStatus
{
    Eligible,
    NotEligible,
    Unknown,
    ProviderUnavailable,
    RequiresManualVerification,
    RequiresPatientConfirmation,
    RequiresFallback
}
