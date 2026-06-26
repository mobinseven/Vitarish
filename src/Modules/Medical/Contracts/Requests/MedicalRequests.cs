using Vitarish.Medical.Contracts.Enums;
namespace Vitarish.Medical.Contracts.Requests;

public sealed record ListCodeSystemsRequest(bool ActiveOnly = true);
public sealed record GetCodeSystemRequest(Guid CodeSystemId);
public sealed record ListCodeSystemReleasesRequest(Guid CodeSystemId, bool ActiveOnly = true);
public sealed record SearchMedicalCodesRequest(string SystemKey, string TextOrCode, Guid? ReleaseId = null, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record SearchPrescriptionReferenceCodesRequest(MedicalPrescriptionItemCategory Category, string TextOrCode, Guid? ReleaseId = null, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record SearchDrugsRequest(string Text, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record SearchMedicalServicesRequest(string Text, string? Category = null, string? Specialty = null, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record ImportCodeSystemReleaseRequest(Guid CodeSystemId, string Version, MedicalImportSourceType SourceType, IReadOnlyList<ImportMedicalCodeItemRequest> Codes);
public sealed record ImportMedicalCodeItemRequest(string Code, string DisplayName, string? Description = null);
public sealed record GetImportBatchRequest(Guid ImportBatchId);
public sealed record GetPrescriptionRequest { public required Guid PrescriptionId { get; init; } }
public sealed record ListPatientPrescriptionsRequest(Guid? PatientId = null, Guid? ClinicId = null, MedicalPrescriptionStatus? StatusFilter = null, string? ProviderKeyFilter = null, DateTime? FromDate = null, DateTime? ToDate = null, int Skip = 0, int Take = 20);
public sealed record CreatePrescriptionDraftRequest(Guid PatientId, string? PatientNationalId, Guid DoctorId, Guid ClinicId);
public sealed record UpdatePrescriptionDraftRequest(Guid PrescriptionId, string? PatientNationalId);
public sealed record AddPrescriptionItemRequest { public Guid PrescriptionId { get; init; } public MedicalPrescriptionItemCategory Category { get; init; } public string? ReferenceCode { get; init; } public string? DisplayName { get; init; } public string? DosageJson { get; init; } public int Quantity { get; init; } public int? DurationDays { get; init; } }
public sealed record UpdatePrescriptionItemRequest { public Guid PrescriptionId { get; init; } public Guid ItemId { get; init; } public string? DosageJson { get; init; } public int Quantity { get; init; } public int? DurationDays { get; init; } }
public sealed record RemovePrescriptionItemRequest(Guid PrescriptionId, Guid ItemId);
public sealed record SelectPrescriptionProviderRequest(Guid PrescriptionId, string ProviderKey);
public sealed record CheckPrescriptionEligibilityRequest(string PatientNationalId, string ProviderKey, Guid DoctorId, Guid ClinicId);
public sealed record SubmitPrescriptionRequest(Guid PrescriptionId, string? IdempotencyKey = null);
public sealed record CancelPrescriptionRequest(Guid PrescriptionId, string Reason);
public sealed record PaperFallbackRequest(Guid PrescriptionId, string Reason);
public sealed record ListPrescriptionTemplatesRequest(string? TenantId = null, MedicalPrescriptionTemplateStatus? Status = null);
public sealed record GetPrescriptionTemplateRequest(Guid TemplateId, string? TenantId = null);
public sealed record SavePrescriptionTemplateRequest(Guid? TemplateId, string TenantId, string Name, string? Description, IReadOnlyList<SavePrescriptionTemplateItemRequest> Items);
public sealed record SavePrescriptionTemplateItemRequest(MedicalPrescriptionItemCategory Category, string? ReferenceCode, string? DisplayName, int Quantity, int? DurationDays);
public sealed record ApplyPrescriptionTemplateRequest(Guid TemplateId, Guid PrescriptionId, string TenantId);
public sealed record ConfigureProviderCredentialRequest(Guid? CredentialId, Guid DoctorId, Guid ClinicId, string ProviderKey, IReadOnlyDictionary<string,string> ConfigurationValues);
public sealed record SyncProviderReferenceDataRequest(string ProviderKey, string CodeSystem, bool ForceRefresh = false);
public sealed record OperationalDashboardRequest(string? ProviderKey = null, DateTime? From = null, DateTime? To = null, Guid? ClinicId = null, int Take = 20);
public sealed record ReconciliationQueueRequest(string? TenantId = null, string? ProviderKey = null, MedicalPrescriptionStatus? StatusFilter = null, int Skip = 0, int Take = 50);
public sealed record OutboxStatusRequest(string? ProviderKey = null, string? TenantId = null);
public sealed record RefreshProviderStatusRequest(Guid PrescriptionId, string ProviderKey);
public sealed record ResolveDuplicateRiskRequest(Guid PrescriptionId, string ResolutionNotes);
public sealed record ReconcilePaperFallbackRequest(Guid PrescriptionId, string PaperTrackingNumber, string Notes);
public sealed record ManualReconcileRequest(Guid PrescriptionId, string RemotePrescriptionId, string Notes);
public sealed record EscalatePrescriptionRequest(Guid PrescriptionId, string Reason);

// Compatibility request names used by existing Interface components during ADR-003 migration.
public sealed record RegisterCodeSystemCommand(string SystemKey, string DisplayName, MedicalCodeSystemCategory Category, string? Description);
public sealed record RegisterCodeSystemReleaseCommand(Guid CodeSystemId, string Version, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record ActivateCodeSystemReleaseCommand(Guid ReleaseId);
public sealed record DeactivateCodeSystemReleaseCommand(Guid ReleaseId);
public sealed record CreateLocalMedicalCodeCommand(Guid CodeSystemReleaseId, string Code, string DisplayName, string? Description, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record CreateCodeMapCommand(Guid SourceMedicalCodeId, Guid TargetMedicalCodeId, string MappingType, string? Notes);
public sealed record CreateMedicalServiceCommand(string Name, string? Category, string? Specialty, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record UpdateLocalMedicalServiceCommand(Guid ServiceId, string Name, string? Category, string? Specialty, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record RetireMedicalServiceCommand(Guid ServiceId);
public sealed record CreateDrugCommand(string PrimaryName, string? PrimaryIdentifierType, string? PrimaryIdentifierValue, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record UpdateLocalDrugCommand(Guid DrugId, string PrimaryName, string? PrimaryIdentifierType, string? PrimaryIdentifierValue, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record RetireDrugCommand(Guid DrugId);
