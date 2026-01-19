using Vitarish.Sea.Interface.Plugins.Base.Enums;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Models;

public sealed record ClinicProjectMetadata
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public DateTime? DateOfBirth { get; init; }

    public DateTime? DateOfSurgery { get; init; }

    public Gender Gender { get; init; }

    public string? PlaceOfBirth { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? InsuranceProvider { get; init; }

    public string? VisitReason { get; init; }

    public string? Allergies { get; init; }

    public string? CurrentMedications { get; init; }

    public string? ClinicalNotes { get; init; }

    public string? EmergencyContactName { get; init; }

    public string? EmergencyContactPhone { get; init; }
}
