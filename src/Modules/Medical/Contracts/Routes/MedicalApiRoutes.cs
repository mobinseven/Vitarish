namespace Vitarish.Medical.Contracts.Routes;

public static class MedicalApiRoutes
{
    public const string Base = "/api/modules/medical";
    public const string CodeSystems = "/code-systems";
    public const string CodeSystemById = "/code-systems/{codeSystemId:guid}";
    public const string CodeSystemReleases = "/code-systems/{codeSystemId:guid}/releases";
    public const string CodeSystemReleaseImport = "/code-system-releases/imports";
    public const string ReferenceSearch = "/reference/search";
    public const string MedicalCodeSearch = "/reference/medical-codes/search";
    public const string PrescriptionReferenceCodeSearch = "/prescriptions/reference-codes/search";
    public const string DrugSearch = "/reference/drugs/search";
    public const string MedicalServiceSearch = "/reference/medical-services/search";
    public const string Imports = "/imports";
    public const string ImportById = "/imports/{importBatchId:guid}";
    public const string Prescriptions = "/prescriptions";
    public const string PrescriptionById = "/prescriptions/{prescriptionId:guid}";
    public const string PrescriptionDrafts = "/prescriptions/drafts";
    public const string PrescriptionHistory = "/prescriptions/history";
    public const string PrescriptionItems = "/prescriptions/{prescriptionId:guid}/items";
    public const string PrescriptionItemById = "/prescriptions/{prescriptionId:guid}/items/{itemId:guid}";
    public const string PrescriptionProviderSelection = "/prescriptions/{prescriptionId:guid}/provider";
    public const string PrescriptionEligibility = "/prescriptions/eligibility";
    public const string PrescriptionSubmission = "/prescriptions/{prescriptionId:guid}/submission";
    public const string PrescriptionCancellation = "/prescriptions/{prescriptionId:guid}/cancellation";
    public const string PaperFallback = "/prescriptions/{prescriptionId:guid}/paper-fallback";
    public const string PrescriptionTemplates = "/prescription-templates";
    public const string PrescriptionTemplateById = "/prescription-templates/{templateId:guid}";
    public const string PrescriptionTemplateApply = "/prescription-templates/{templateId:guid}/apply";
    public const string Credentials = "/credentials";
    public const string CredentialById = "/credentials/{credentialId:guid}";
    public const string ProviderReferenceDataSync = "/providers/reference-data/sync";
    public const string ProviderResults = "/providers/results";
    public const string ImportResults = "/imports/results";
    public const string ObservabilityDashboard = "/observability/dashboard";
    public const string Reconciliation = "/reconciliation";
    public const string OutboxStatus = "/outbox/status";
    public const string RetryStatus = "/outbox/retry-status";
    public const string Recovery = "/recovery";
    public const string Escalation = "/recovery/escalation";
}
