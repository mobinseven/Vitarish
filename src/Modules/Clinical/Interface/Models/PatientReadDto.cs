using Vitarish.Clinical.Interface.Enums;

namespace Vitarish.Clinical.Interface.Models;

internal record PatientReadDto : ProjectReadDto
{
    public string? FirstName => GetField<string>();

    public string? LastName => GetField<string>();

    public DateTime? DateOfBirth => GetField<DateTime?>();

    public DateTime? DateOfSurgery => GetField<DateTime?>();

    public Gender Gender => GetField<Gender>();

    public string? PlaceOfBirth => GetField<string>();

    public string? Email => GetField<string>();

    public string? Phone => GetField<string>();

    public string? InsuranceProvider => GetField<string>();

    public string? VisitReason => GetField<string>();

    public string? Allergies => GetField<string>();

    public string? CurrentMedications => GetField<string>();

    public string? ClinicalNotes => GetField<string>();

    public string? EmergencyContactName => GetField<string>();

    public string? EmergencyContactPhone => GetField<string>();
}
