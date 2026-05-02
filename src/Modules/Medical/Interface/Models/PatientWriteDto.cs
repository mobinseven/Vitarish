namespace Vitarish.Medical.Interface.Models;

internal record PatientWriteDto : ProjectWriteDto
{
    public string? FirstName { get => GetField<string>(); set => SetField(value); }

    public string? LastName { get => GetField<string>(); set => SetField(value); }

    public DateTime? DateOfBirth { get => GetField<DateTime?>(); set => SetField(value); }

    public DateTime? DateOfSurgery { get => GetField<DateTime?>(); set => SetField(value); }

    public Gender Gender { get => GetField<Gender>(); set => SetField(value); }

    public string? PlaceOfBirth { get => GetField<string>(); set => SetField(value); }

    public string? Email { get => GetField<string>(); set => SetField(value); }

    public string? Phone { get => GetField<string>(); set => SetField(value); }

    public string? InsuranceProvider { get => GetField<string>(); set => SetField(value); }

    public string? VisitReason { get => GetField<string>(); set => SetField(value); }

    public string? Allergies { get => GetField<string>(); set => SetField(value); }

    public string? CurrentMedications { get => GetField<string>(); set => SetField(value); }

    public string? ClinicalNotes { get => GetField<string>(); set => SetField(value); }

    public string? EmergencyContactName { get => GetField<string>(); set => SetField(value); }

    public string? EmergencyContactPhone { get => GetField<string>(); set => SetField(value); }

    public string? LinkedUserId { get => GetField<string>(); set => SetField(value); }
}
