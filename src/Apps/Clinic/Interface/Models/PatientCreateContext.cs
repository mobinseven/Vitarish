namespace Vitarish.Clinic.Interface.Models;

public class PatientCreateContext
{
    public IReadOnlyList<ProjectReadDto> DuplicateCandidates { get; set; } = [];

    public ProjectWriteDto? NewPatientDraft { get; set; }

    public void Reset()
    {
        DuplicateCandidates = [];
        NewPatientDraft = null;
    }
}
