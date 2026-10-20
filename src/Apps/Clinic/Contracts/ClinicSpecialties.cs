namespace Vitarish.Clinic.Contracts;

/// <summary>
/// The specialty codes a clinic can choose as its business category. They are stored and sent as
/// is, and the Web localizes each one, so codes are only ever added, never renamed or removed.
/// </summary>
public static class ClinicSpecialties
{
    public const string PlasticSurgery = "plastic-surgery";
    public const string Dermatology = "dermatology";
    public const string Dentistry = "dentistry";
    public const string Ophthalmology = "ophthalmology";
    public const string Otolaryngology = "otolaryngology";
    public const string GeneralSurgery = "general-surgery";
    public const string Orthopedics = "orthopedics";
    public const string ObstetricsGynecology = "obstetrics-gynecology";
    public const string Urology = "urology";
    public const string Cardiology = "cardiology";
    public const string InternalMedicine = "internal-medicine";
    public const string Pediatrics = "pediatrics";
    public const string GeneralPractice = "general-practice";
    public const string Psychiatry = "psychiatry";
    public const string Physiotherapy = "physiotherapy";
    public const string Nutrition = "nutrition";
    public const string Other = "other";

    /// <summary>Every specialty in the order the Web offers them, with <see cref="Other"/> last.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        PlasticSurgery,
        Dermatology,
        Dentistry,
        Ophthalmology,
        Otolaryngology,
        GeneralSurgery,
        Orthopedics,
        ObstetricsGynecology,
        Urology,
        Cardiology,
        InternalMedicine,
        Pediatrics,
        GeneralPractice,
        Psychiatry,
        Physiotherapy,
        Nutrition,
        Other
    ];
}
