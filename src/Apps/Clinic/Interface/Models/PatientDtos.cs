namespace Vitarish.Clinic.Interface.Models;

public record PatientWriteDto : ProjectWriteDto
{
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

public record PatientReadDto : ProjectReadDto
{
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
