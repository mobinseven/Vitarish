namespace Vitarish.Clinic.Interface.Components.Patients;

public class PatientAdvancedSearchContext
{
    /// <summary>The IdentificationCode typed by the user (primary lookup key).</summary>
    public string? IdentificationCode { get; set; }

    /// <summary>Advanced search patient-name query.</summary>
    public string? SearchQuery { get; set; }

    /// <summary>Advanced search identification code.</summary>
    public string? SearchIdentificationCode { get; set; }

    /// <summary>Advanced search date of birth.</summary>
    public DateTime? SearchDateOfBirth { get; set; }

    /// <summary>Advanced search phone number.</summary>
    public string? SearchPhone { get; set; }

    /// <summary>Advanced search email address.</summary>
    public string? SearchEmail { get; set; }

    public ProjectReadDto? ExactIdentificationCodeMatch { get; set; }

    public bool IdentificationLookupInProgress { get; set; }

    public bool IdentificationLookupDone { get; set; }

    public bool AdvancedSearchDone { get; set; }

    public IReadOnlyList<ProjectReadDto> SearchResults { get; set; } = [];

    public void Reset()
    {
        IdentificationCode = null;
        SearchQuery = null;
        SearchIdentificationCode = null;
        SearchDateOfBirth = null;
        SearchPhone = null;
        SearchEmail = null;
        ExactIdentificationCodeMatch = null;
        IdentificationLookupInProgress = false;
        IdentificationLookupDone = false;
        AdvancedSearchDone = false;
        SearchResults = [];
    }
}
