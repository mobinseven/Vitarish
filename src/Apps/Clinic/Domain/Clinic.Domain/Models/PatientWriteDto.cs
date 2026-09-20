using Vitarish.Clinic.Domain.Enums;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Domain.Models;

public record PatientWriteDto : ProjectWriteDto
{
    public string[] Tags { get => GetField<string[]>() ?? []; set => SetField(PatientTags.Normalize(value)); }

    public new Guid? AvatarFileId { get => GetField<Guid?>(); set => SetField(value); }

    public string? FirstName { get => GetField<string>(); set => SetField(value); }

    public string? LastName { get => GetField<string>(); set => SetField(value); }

    public DateTime? DateOfBirth { get => GetField<DateTime?>(); set => SetField(value); }

    public DateTime? DateOfSurgery { get => GetField<DateTime?>(); set => SetField(value); }

    public Gender Gender { get => GetField<Gender>(); set => SetField(value); }

    public string? PlaceOfBirth { get => GetField<string>(); set => SetField(value); }

    public string? Address { get => GetField<string>(); set => SetField(value); }

    public Education Education { get => GetField<Education>(); set => SetField(value); }

    public string? Occupation { get => GetField<string>(); set => SetField(value); }

    public string? FatherName { get => GetField<string>(); set => SetField(value); }

    public int Height { get => GetField<int>(); set => SetField(value); }

    public int Weight { get => GetField<int>(); set => SetField(value); }

    public string? Email { get => GetField<string>(); set => SetField(value); }

    public string? Phone { get => GetField<string>(); set => SetField(value); }

    public string? Referrer { get => GetField<string>(); set => SetField(value); }

    public string? InsuranceProvider { get => GetField<string>(); set => SetField(value); }

    public string? VisitReason { get => GetField<string>(); set => SetField(value); }

    public string? Allergies { get => GetField<string>(); set => SetField(value); }

    public string? CurrentMedications { get => GetField<string>(); set => SetField(value); }

    public string? ClinicalNotes { get => GetField<string>(); set => SetField(value); }

    public string? EmergencyContactName { get => GetField<string>(); set => SetField(value); }

    public string? EmergencyContactPhone { get => GetField<string>(); set => SetField(value); }
}
