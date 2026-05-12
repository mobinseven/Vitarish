namespace Vitarish.Medical.Interface.Components.Patients;

public class PatientFindOrCreateContext
{
    /// <summary>The IdentificationCode typed by the user (primary lookup key).</summary>
    public string? IdentificationCode { get; set; }

    /// <summary>Advanced search query (name / phone / DoB).</summary>
    public string? SearchQuery { get; set; }

    public IReadOnlyList<ProjectReadDto> SearchResults { get; set; } = [];

    public IReadOnlyList<ProjectReadDto> DuplicateCandidates { get; set; } = [];

    public ProjectWriteDto? NewPatientDraft { get; set; }

    public void Reset()
    {
        IdentificationCode = null;
        SearchQuery = null;
        SearchResults = [];
        DuplicateCandidates = [];
        NewPatientDraft = null;
    }
}
