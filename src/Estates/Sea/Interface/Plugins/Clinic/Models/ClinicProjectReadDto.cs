using Vitarish.Sea.Interface.Plugins.Base.Enums;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Models;

internal record ClinicProjectReadDto : ProjectReadDto
{
    public static ClinicProjectReadDto? ToRead(ProjectReadDto project) => JsonSerializer.Deserialize<ClinicProjectReadDto>(JsonSerializer.Serialize(project));

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
