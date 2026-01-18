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

    public string? DateOfBirth
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

    public string? PolicyNumber
    {
        get;
        set => SetField(ref field, value);
    }

    public string? PrimaryPhysician
    {
        get;
        set => SetField(ref field, value);
    }

    public string? VisitReason
    {
        get;
        set => SetField(ref field, value);
    }

    public string? PreferredPharmacy
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

    public string? DepartmentName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ConsentToShareRecords
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public void LoadFrom(ClinicProjectMetadata metadata)
    {
        suppressNotifications = true;
        FirstName = metadata.FirstName;
        LastName = metadata.LastName;
        DateOfBirth = metadata.DateOfBirth;
        DateOfSurgery = metadata.DateOfSurgery;
        Gender = metadata.Gender;
        Email = metadata.Email;
        Phone = metadata.Phone;
        InsuranceProvider = metadata.InsuranceProvider;
        PolicyNumber = metadata.PolicyNumber;
        PrimaryPhysician = metadata.PrimaryPhysician;
        VisitReason = metadata.VisitReason;
        PreferredPharmacy = metadata.PreferredPharmacy;
        Allergies = metadata.Allergies;
        CurrentMedications = metadata.CurrentMedications;
        ClinicalNotes = metadata.ClinicalNotes;
        EmergencyContactName = metadata.EmergencyContactName;
        EmergencyContactPhone = metadata.EmergencyContactPhone;
        DepartmentName = metadata.DepartmentName;
        ConsentToShareRecords = metadata.ConsentToShareRecords ?? true;
        suppressNotifications = false;
    }

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
        PolicyNumber = null;
        PrimaryPhysician = null;
        VisitReason = null;
        PreferredPharmacy = null;
        Allergies = null;
        CurrentMedications = null;
        ClinicalNotes = null;
        EmergencyContactName = null;
        EmergencyContactPhone = null;
        DepartmentName = null;
        ConsentToShareRecords = true;
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
}
