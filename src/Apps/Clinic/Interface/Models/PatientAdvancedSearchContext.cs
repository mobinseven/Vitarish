namespace Vitarish.Clinic.Interface.Models;

public enum PatientIdentificationLookupState
{
    None = 0,
    LookingUp = 1,
    Found = 2,
    NotFound = 3,
    Failed = 4
}

public class PatientAdvancedSearchContext
{
    /// <summary>The IdentificationCode typed by the user (primary lookup key).</summary>
    public string? IdentificationCode { get; set; }

    /// <summary>The normalized IdentificationCode used by the latest completed primary lookup.</summary>
    public string? LastLookedUpIdentificationCode { get; set; }

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

    public PatientIdentificationLookupState IdentificationLookupState { get; set; } = PatientIdentificationLookupState.None;

    public bool IdentificationLookupInProgress { get; set; }

    public bool IdentificationLookupDone { get; set; }

    public string? IdentificationLookupError { get; set; }

    public bool AdvancedSearchInProgress { get; set; }

    public bool AdvancedSearchDone { get; set; }

    public IReadOnlyList<ProjectReadDto> SearchResults { get; set; } = [];

    public bool HasIdentificationCode => !string.IsNullOrWhiteSpace(IdentificationCode);

    public bool IsIdentificationLookupIdle => IdentificationLookupState == PatientIdentificationLookupState.None;

    public bool IsIdentificationLookupLookingUp => IdentificationLookupState == PatientIdentificationLookupState.LookingUp;

    public bool IsIdentificationLookupFound
        => IdentificationLookupState == PatientIdentificationLookupState.Found
           && ExactIdentificationCodeMatch is not null;

    public bool IsIdentificationLookupNotFound
        => IdentificationLookupState == PatientIdentificationLookupState.NotFound
           && IdentificationLookupDone;

    public bool IsIdentificationLookupFailed
        => IdentificationLookupState == PatientIdentificationLookupState.Failed;

    public void Reset()
    {
        IdentificationCode = null;
        LastLookedUpIdentificationCode = null;
        ClearAdvancedSearchCriteria();
        ClearLookupOutcome();
    }

    public void PrepareForIdentificationCodeInput(string? identificationCode)
    {
        IdentificationCode = identificationCode;
        LastLookedUpIdentificationCode = null;
        ClearAdvancedSearchCriteria();
        ClearLookupOutcome();
    }

    public void BeginIdentificationLookup(string identificationCode)
    {
        LastLookedUpIdentificationCode = identificationCode;
        ExactIdentificationCodeMatch = null;
        IdentificationLookupState = PatientIdentificationLookupState.LookingUp;
        IdentificationLookupInProgress = true;
        IdentificationLookupDone = false;
        IdentificationLookupError = null;
        AdvancedSearchDone = false;
        AdvancedSearchInProgress = false;
        SearchResults = [];
    }

    public void CompleteIdentificationLookup(ProjectReadDto? exactMatch, string identificationCode)
    {
        LastLookedUpIdentificationCode = identificationCode;
        ExactIdentificationCodeMatch = exactMatch;
        IdentificationLookupState = exactMatch is null
            ? PatientIdentificationLookupState.NotFound
            : PatientIdentificationLookupState.Found;
        IdentificationLookupInProgress = false;
        IdentificationLookupDone = true;
        IdentificationLookupError = null;
        AdvancedSearchDone = false;
        AdvancedSearchInProgress = false;
        SearchResults = [];

        if (exactMatch is null)
        {
            SearchIdentificationCode = identificationCode;
        }
    }

    public void FailIdentificationLookup(string identificationCode, string? error = null)
    {
        LastLookedUpIdentificationCode = identificationCode;
        ExactIdentificationCodeMatch = null;
        IdentificationLookupState = PatientIdentificationLookupState.Failed;
        IdentificationLookupInProgress = false;
        IdentificationLookupDone = true;
        IdentificationLookupError = error;
        AdvancedSearchDone = false;
        AdvancedSearchInProgress = false;
        SearchResults = [];
    }

    public void CancelIdentificationLookup()
    {
        if (IdentificationLookupState == PatientIdentificationLookupState.LookingUp)
        {
            ClearLookupOutcome();
        }
    }

    public void BeginAdvancedSearch()
    {
        AdvancedSearchInProgress = true;
        AdvancedSearchDone = false;
        SearchResults = [];
    }

    public void CompleteAdvancedSearch(IReadOnlyList<ProjectReadDto> results)
    {
        SearchResults = results;
        AdvancedSearchInProgress = false;
        AdvancedSearchDone = true;
    }

    public void FailAdvancedSearch()
    {
        SearchResults = [];
        AdvancedSearchInProgress = false;
        AdvancedSearchDone = true;
    }

    public void ClearAdvancedSearchCriteria()
    {
        SearchQuery = null;
        SearchIdentificationCode = null;
        SearchDateOfBirth = null;
        SearchPhone = null;
        SearchEmail = null;
        AdvancedSearchInProgress = false;
        AdvancedSearchDone = false;
        SearchResults = [];
    }

    private void ClearLookupOutcome()
    {
        ExactIdentificationCodeMatch = null;
        IdentificationLookupState = PatientIdentificationLookupState.None;
        IdentificationLookupInProgress = false;
        IdentificationLookupDone = false;
        IdentificationLookupError = null;
        AdvancedSearchInProgress = false;
        AdvancedSearchDone = false;
        SearchResults = [];
    }
}
