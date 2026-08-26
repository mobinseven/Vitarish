namespace Vitarish.Clinic.Contracts.Routes;

public static class ClinicApiRoutes
{
    public const string Base = "/api/modules/clinic";
    public const string Patients = "/patients";
    public const string PatientCount = "/patients/count";
    public const string PatientSearch = "/patients/search";
    public const string PatientIdentificationLookup = "/patients/identification-lookup";
    public const string PatientFindOrCreate = "/patients/find-or-create";
    public const string PatientDetails = "/patients/{patientId:guid}";
    public const string PatientUpdate = PatientDetails;
    public const string SurgeryCatalogs = "/surgeries/catalogs";
    public const string PatientSurgeries = PatientDetails + "/surgeries";
    public const string Surgeries = "/surgeries";
    public const string DashboardAppointments = "/dashboard/appointments";
    public const string DashboardSurgeries = "/dashboard/surgeries";
    public const string SurgeryById = Surgeries + "/{id:guid}";
    public const string SurgeryCancel = SurgeryById + "/cancel";
    public const string PatientAssistant = "/patients/{patientId:guid}/assistant";
    // Compatibility window owned by Clinic API: legacy Web/third-party clients and persisted Intelligence conversations remain dependents;
    // remove these aliases only after all clients migrate and legacy traffic plus legacy conversation reads are absent.
    public const string LegacyPatientAgent = "/patients/{patientId:guid}/agent";
    public const string PatientAssistantConversation = PatientAssistant + "/{conversationId:guid}";
    public const string PatientAssistantAction = PatientAssistant + "/actions/{actionId:guid}";
    public const string LegacyPatientAgentAction = LegacyPatientAgent + "/actions/{actionId:guid}";
    public const string ConfirmPatientAssistantAction = PatientAssistantAction + "/confirm";
    public const string ConfirmLegacyPatientAgentAction = LegacyPatientAgentAction + "/confirm";
    public const string RejectPatientAssistantAction = PatientAssistantAction + "/reject";
    public const string RejectLegacyPatientAgentAction = LegacyPatientAgentAction + "/reject";
    public const string AssistantActions = "/assistant/actions/{actionId:guid}";
    public const string LegacyAgentActions = "/agent/actions/{actionId:guid}";
    public const string AssistantAction = AssistantActions;
    public const string ConfirmAssistantAction = AssistantActions + "/confirm";
    public const string RejectAssistantAction = AssistantActions + "/reject";
    public const string OperationsAssistant = "/assistant/operations";
    public const string LegacyOperationsAgent = "/agent/operations";
    public const string OperationsAssistantConversation = OperationsAssistant + "/{conversationId:guid}";
    public const string ResolveClinicAssistantCandidate = "/assistant/patient-candidates/resolve";
    public const string LegacyResolvePatientAgentCandidate = "/agent/patient-candidates/resolve";
    public const string StartVisitPrescriptionDraft = "/visits/prescription-drafts/start";
    public const string Health = "/health";
    public const string InternalAssistantTools = Base + "/internal/assistant/tools/execute";
    public const string InternalAssistantHealth = Base + "/internal/assistant/health";
    public const string LegacyInternalAgentTools = Base + "/internal/agent/tools/execute";
    public const string LegacyInternalAgentHealth = Base + "/internal/agent/health";
}
