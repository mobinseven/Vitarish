using Vitarish.Sea.Interface.Plugins.Base.Enums;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Models;

public sealed class ClinicProjectMetadataModel(Action onChanged)
{
    private readonly Action onChanged = onChanged;
    private bool suppressNotifications;

    public string? FirstName
    {
        get;
        set => SetField(ref field, value);
    }

    public string? LastName
    {
        get;
        set => SetField(ref field, value);
    }

    public DateTime? DateOfBirth
    {
        get;
        set => SetField(ref field, value);
    }
    public DateTime? DateOfSurgery
    {
        get;
        set => SetField(ref field, value);
    }

    public Gender Gender
    {
        get;
        set => SetField(ref field, value);
    }

    public string? PlaceOfBirth
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Email
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Phone
    {
        get;
        set => SetField(ref field, value);
    }

    public string? InsuranceProvider
    {
        get;
        set => SetField(ref field, value);
    }

    public string? VisitReason
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Allergies
    {
        get;
        set => SetField(ref field, value);
    }

    public string? CurrentMedications
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ClinicalNotes
    {
        get;
        set => SetField(ref field, value);
    }

    public string? EmergencyContactName
    {
        get;
        set => SetField(ref field, value);
    }

    public string? EmergencyContactPhone
    {
        get;
        set => SetField(ref field, value);
    }
    public void LoadFrom(ClinicProjectMetadata metadata)
    {
        suppressNotifications = true;
        FirstName = metadata.FirstName;
        LastName = metadata.LastName;
        DateOfBirth = metadata.DateOfBirth;
        DateOfSurgery = metadata.DateOfSurgery;
        Gender = metadata.Gender;
        PlaceOfBirth  = metadata.PlaceOfBirth;
        Email = metadata.Email;
        Phone = metadata.Phone;
        InsuranceProvider = metadata.InsuranceProvider;
        VisitReason = metadata.VisitReason;
        Allergies = metadata.Allergies;
        CurrentMedications = metadata.CurrentMedications;
        ClinicalNotes = metadata.ClinicalNotes;
        EmergencyContactName = metadata.EmergencyContactName;
        EmergencyContactPhone = metadata.EmergencyContactPhone;
        suppressNotifications = false;
    }

    public ClinicProjectMetadata GetModel() => new()
    {
        FirstName = NormalizeInput(FirstName),
        LastName = NormalizeInput(LastName),
        DateOfBirth = DateOfBirth,
        DateOfSurgery = DateOfSurgery,
        Gender = Gender,
        PlaceOfBirth = NormalizeInput(PlaceOfBirth),
        Email = NormalizeInput(Email),
        Phone = NormalizeInput(Phone),
        InsuranceProvider = NormalizeInput(InsuranceProvider),
        VisitReason = NormalizeInput(VisitReason),
        Allergies = NormalizeInput(Allergies),
        CurrentMedications = NormalizeInput(CurrentMedications),
        ClinicalNotes = NormalizeInput(ClinicalNotes),
        EmergencyContactName = NormalizeInput(EmergencyContactName),
        EmergencyContactPhone = NormalizeInput(EmergencyContactPhone),
    };

    public void Clear()
    {
        suppressNotifications = true;
        FirstName = null;
        LastName = null;
        DateOfBirth = null;
        Gender = default;
        Email = null;
        Phone = null;
        InsuranceProvider = null;
        VisitReason = null;
        Allergies = null;
        CurrentMedications = null;
        ClinicalNotes = null;
        EmergencyContactName = null;
        EmergencyContactPhone = null;
        suppressNotifications = false;
    }

    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;

        if (!suppressNotifications)
        {
            onChanged();
        }
    }

    /// <summary>
    /// Normalizes string input by trimming whitespace and converting empty strings to null.
    /// </summary>
    /// <param name="value">The raw input.</param>
    /// <returns>The normalized value.</returns>
    private static string? NormalizeInput(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
