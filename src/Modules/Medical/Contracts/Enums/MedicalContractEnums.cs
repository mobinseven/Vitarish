namespace Vitarish.Medical.Contracts.Enums;

public enum MedicalCodeStatus { Draft, Active, Deprecated, Retired }
public enum MedicalServiceStatus { Draft, Active, Deprecated, Retired }
public enum DrugStatus { Draft, Active, Discontinued, Retired }
public enum MedicalImportBatchStatus { Pending, Processing, Completed, CompletedWithWarnings, Failed, Cancelled }
public enum MedicalImportSourceType { Manual, Cms, Provider, File, Api }
public enum MedicalCodeSystemCategory { Diagnosis, Procedure, Drug, Laboratory, Imaging, Service, Other }
public enum MedicalCodeMapStatus { Draft, Active, Deprecated, Retired }
public enum MedicalPrescriptionStatus { Draft, ReadyForSubmission, Submitting, Submitted, AcceptedByProvider, RejectedByProvider, NeedsCorrection, EditRequested, Edited, CancelRequested, Cancelled, PaperFallback, SyncPending, SyncFailed, DuplicateRisk, ProviderUnavailable, ManuallyReconciled }
public enum MedicalPrescriptionTemplateStatus { Draft, Active, Archived }
public enum MedicalPrescriptionItemCategory { Drug, Lab, Imaging, Visit, Referral, MedicalService, MedicalEquipment }
public enum MedicalEligibilityStatus { Unknown, Eligible, Ineligible, NotEligible, Pending, RequiresManualVerification, RequiresPatientConfirmation, RequiresFallback, ProviderUnavailable, Rejected }
public enum MedicalCredentialStatus { Pending, Active, ExpiringSoon, Expired, Revoked, Invalid }
public enum MedicalInsuranceProviderCapabilityType { EligibilityCheck, PrescriptionSubmission, PrescriptionCancellation, ReferenceDataSync, SupportsDrugPrescription, SupportsMedicalServicePrescription }
public enum MedicalProviderStatus { Available, Degraded, Unavailable, Maintenance }
public enum MedicalProviderFailureCategory { Validation, Authentication, Authorization, Timeout, Outage, Rejection, RetryExhausted, Unexpected }

public enum MappingType { Equivalent, Narrower, Broader, Related, LocalOverride }
public enum RecordOrigin { Imported, Local }
public enum DrugIdentifierType { Local, Ndc, RxNorm, Gtin }
public enum DuplicateRiskResolution { ConfirmedAccepted, ConfirmedNotSubmitted }
