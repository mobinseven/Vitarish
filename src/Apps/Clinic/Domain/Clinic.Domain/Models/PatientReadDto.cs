using Vitarish.Clinic.Domain.Enums;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Domain.Models;

public record PatientReadDto : ProjectReadDto
{
    public string[] Tags => GetField<string[]>() ?? [];

    public string? FirstName => GetField<string>();

    public string? LastName => GetField<string>();

    public DateTime? DateOfBirth => GetField<DateTime?>();

    public DateTime? DateOfSurgery => GetField<DateTime?>();

    public Gender Gender => GetField<Gender>();

    public string? PlaceOfBirth => GetField<string>();

    public string? Address => GetField<string>();

    public Education Education => GetField<Education>();

    public string? Occupation => GetField<string>();

    public string? FatherName => GetField<string>();

    public int Height => GetField<int>();

    public int Weight => GetField<int>();

    public string? Email => GetField<string>();

    public string? Phone => GetField<string>();

    public string? Referrer => GetField<string>();

    public string? InsuranceProvider => GetField<string>();

    public string? VisitReason => GetField<string>();

    public string? Allergies => GetField<string>();

    public string? CurrentMedications => GetField<string>();

    public string? ClinicalNotes => GetField<string>();

    public string? EmergencyContactName => GetField<string>();

    public string? EmergencyContactPhone => GetField<string>();
}
