namespace Vitarish.Clinic.Application.Assistant.Capabilities;

/// <summary>Names of the Clinic assistant capabilities over patients' procedures and steps.</summary>
public static class ProcedureCapabilityNames
{
    public const string Reads = "ProcedureReads";
    public const string OperationsReads = "OperationsProcedureReads";
    public const string Changes = "ProcedureChanges";
    public const string OperationsChanges = "OperationsProcedureChanges";
    public const string CareTeamAccess = "CareTeamAccess";
}

/// <summary>Action types of prepared changes to patients' procedures and steps; each assistant has its own.</summary>
public static class ProcedureActionTypes
{
    public const string StepChange = "procedure_step_change";
    public const string Create = "procedure_create";
    public const string OperationsStepChange = "operations_procedure_step_change";
    public const string OperationsCreate = "operations_procedure_create";
    public const string Workflow = "procedure-changes";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { StepChange, Create, OperationsStepChange, OperationsCreate };
}

/// <summary>What a prepared procedure change does; the review card is chosen by it.</summary>
public static class ProcedureChangeKinds
{
    public const string CommitStep = "commit_step";
    public const string ReopenStep = "reopen_step";
    public const string AddNote = "add_note";
    public const string AddReminder = "add_reminder";
    public const string AddProcedure = "add_procedure";
}

/// <summary>Model-facing names of the clinical event tools, which add a documented visit as a procedure step.</summary>
public static class ClinicalEventToolNames
{
    public const string Prepare = "prepare_clinical_event";
    public const string ProcedureContext = "get_patient_procedure_context";
}
