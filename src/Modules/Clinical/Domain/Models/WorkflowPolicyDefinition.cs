namespace Vitarish.Clinical.Domain.Models;

public sealed record WorkflowPolicyDefinition
{
    public Dictionary<ClinicalActionType, string[]> AllowedRolesByActionType { get; init; } = [];
    public Dictionary<VisitWorkflowStatus, VisitWorkflowStatus[]> AllowedTransitionsByStatus { get; init; } = [];
    public Dictionary<VisitWorkflowStatus, ClinicalActionType[]> RequiredStepsByStatus { get; init; } = [];
    public Dictionary<ClinicalActionType, string[]> RequiredFieldsByActionType { get; init; } = [];
    public Dictionary<ClinicalActionType, string[]> RequiredCodesByActionType { get; init; } = [];
    public bool NotesRequireSignature { get; init; } = true;
    public bool AllowReopenAfterCancel { get; init; }

    public static WorkflowPolicyDefinition CreateDefault() => new()
    {
        AllowedRolesByActionType = new Dictionary<ClinicalActionType, string[]>
        {
            [ClinicalActionType.RegisterPatient] = ["Receptionist", "TenantAdmin"],
            [ClinicalActionType.CheckInPatient] = ["Receptionist", "Nurse", "TenantAdmin"],
            [ClinicalActionType.StartEncounter] = ["Nurse", "Clinician", "TenantAdmin"],
            [ClinicalActionType.RecordVitals] = ["Nurse", "Clinician", "TenantAdmin"],
            [ClinicalActionType.AddDiagnosis] = ["Clinician", "TenantAdmin"],
            [ClinicalActionType.AddMedicationRequest] = ["Clinician", "TenantAdmin"],
            [ClinicalActionType.AddClinicalNote] = ["Nurse", "Clinician", "TenantAdmin"],
            [ClinicalActionType.SignNote] = ["Clinician", "TenantAdmin"],
            [ClinicalActionType.CompleteVisit] = ["Clinician", "TenantAdmin"],
            [ClinicalActionType.CancelVisit] = ["Receptionist", "Nurse", "Clinician", "TenantAdmin"],
            [ClinicalActionType.ReopenVisit] = ["TenantAdmin"]
        },
        AllowedTransitionsByStatus = new Dictionary<VisitWorkflowStatus, VisitWorkflowStatus[]>
        {
            [VisitWorkflowStatus.Draft] = [VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.Cancelled],
            [VisitWorkflowStatus.CheckedIn] = [VisitWorkflowStatus.InTriage, VisitWorkflowStatus.Cancelled],
            [VisitWorkflowStatus.InTriage] = [VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.Cancelled],
            [VisitWorkflowStatus.ReadyForProvider] = [VisitWorkflowStatus.InProgress, VisitWorkflowStatus.Cancelled],
            [VisitWorkflowStatus.InProgress] = [VisitWorkflowStatus.Signed, VisitWorkflowStatus.Completed, VisitWorkflowStatus.Cancelled],
            [VisitWorkflowStatus.Signed] = [VisitWorkflowStatus.Completed],
            [VisitWorkflowStatus.Completed] = [],
            [VisitWorkflowStatus.Cancelled] = []
        },
        NotesRequireSignature = true,
        AllowReopenAfterCancel = false
    };
}
