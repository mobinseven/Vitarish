namespace Vitarish.Clinical.Domain.Models;

public enum ClinicalActionType
{
    RegisterPatient = 0,
    CheckInPatient = 1,
    StartEncounter = 2,
    RecordVitals = 3,
    AddDiagnosis = 4,
    AddMedicationRequest = 5,
    AddClinicalNote = 6,
    SignNote = 7,
    CompleteVisit = 8,
    CancelVisit = 9,
    ReopenVisit = 10
}
