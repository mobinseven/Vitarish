namespace Vitarish.Medical.Interface.Components.Patients;

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
