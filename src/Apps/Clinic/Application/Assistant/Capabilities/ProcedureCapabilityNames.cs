namespace Vitarish.Clinic.Application.Assistant.Capabilities;

/// <summary>Names of the Clinic assistant capabilities over patients' procedures and steps.</summary>
public static class ProcedureCapabilityNames
{
    public const string Reads = "ProcedureReads";
    public const string OperationsReads = "OperationsProcedureReads";
}

/// <summary>Model-facing names of the clinical event tools, which add a documented visit as a procedure step.</summary>
public static class ClinicalEventToolNames
{
    public const string Prepare = "prepare_clinical_event";
    public const string ProcedureContext = "get_patient_procedure_context";
}
