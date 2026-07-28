using Vitarish.Medical.Contracts.Enums;
namespace Vitarish.Medical.Contracts.Requests;

public sealed record ListCodeSystemsRequest(bool ActiveOnly = true);
public sealed record GetCodeSystemRequest(Guid CodeSystemId);
public sealed record ListCodeSystemReleasesRequest(Guid CodeSystemId, bool ActiveOnly = true);
public sealed record SearchMedicalCodesRequest(string SystemKey, string TextOrCode, Guid? ReleaseId = null, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record SearchPrescriptionReferenceCodesRequest(string ProviderKey, string TextOrCode, MedicalPrescriptionItemCategory? Category = null, Guid? ReleaseId = null, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record SearchProviderReferenceDataRequest(string ProviderKey, string CodeSystem, string Text, string? ProviderCategory = null, int Limit = 20);
public sealed record SearchDrugsRequest(string Text, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record SearchMedicalServicesRequest(string Text, string? Category = null, string? Specialty = null, bool ActiveOnly = true, DateOnly? AsOfDate = null, int Limit = 20);
public sealed record ImportCodeSystemReleaseRequest(Guid CodeSystemId, string Version, MedicalImportSourceType SourceType, IReadOnlyList<ImportMedicalCodeItemRequest> Codes);
public sealed record ImportMedicalCodeItemRequest(string Code, string DisplayName, string? Description = null);
public sealed record GetImportBatchRequest(Guid ImportBatchId);
public sealed record GetPrescriptionRequest { public required Guid PrescriptionId { get; init; } }
public sealed record ListPatientPrescriptionsRequest(Guid? PatientId = null, Guid? EncounterId = null, Guid? ClinicId = null, MedicalPrescriptionStatus? StatusFilter = null, string? ProviderKeyFilter = null, DateTime? FromDate = null, DateTime? ToDate = null, int Skip = 0, int Take = 20);
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
public sealed record BeginProviderAuthorizationRequest(Guid CredentialId);
public sealed record CompleteProviderAuthorizationRequest(Guid CredentialId, string AuthorizationCode, string State);
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
public sealed record ImportCodeSystemReleaseCommand(Guid CodeSystemId, string Version, string? Description, DateOnly? EffectiveFrom, DateOnly? EffectiveTo, string SourceLocator, string SourceVersion, string? SourceChecksum, MedicalImportSourceType SourceType, IReadOnlyList<ImportMedicalCodeItemRequest> Codes, bool ValidateOnly);
public sealed record RegisterCodeSystemCommand(string SystemKey, string DisplayName, string? Authority, MedicalCodeSystemCategory Category, string? CanonicalUri, string? Description);
public sealed record RegisterCodeSystemReleaseCommand(Guid CodeSystemId, string Version, DateOnly? ReleaseDate = null, DateOnly? EffectiveFrom = null, DateOnly? EffectiveTo = null, string? SourceLocator = null, string? SourceVersion = null);
public sealed record ActivateCodeSystemReleaseCommand(Guid ReleaseId);
public sealed record DeactivateCodeSystemReleaseCommand(Guid ReleaseId);
public sealed record CreateLocalMedicalCodeCommand(Guid CodeSystemReleaseId, string Code, string DisplayName, string? Description, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record CreateCodeMapCommand(Guid SourceMedicalCodeId, Guid TargetMedicalCodeId, MappingType MappingType, decimal? Confidence = null, DateOnly? EffectiveFrom = null, DateOnly? EffectiveTo = null, string? Notes = null);
public sealed record CreateMedicalServiceCommand(string Name, string? Description, string? Category, string? Specialty, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record UpdateLocalMedicalServiceCommand(Guid ServiceId, string Name, string? Description, string? Category, string? Specialty, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record RetireMedicalServiceCommand(Guid ServiceId);
public sealed record CreateDrugCommand(string PrimaryName, DrugIdentifierType PrimaryIdentifierType, string? PrimaryIdentifierValue, DateOnly? EffectiveFrom, DateOnly? EffectiveTo);
public sealed record UpdateLocalDrugCommand(Guid DrugId, string PrimaryName, DateOnly? EffectiveFrom, DateOnly? EffectiveTo, string? PrimaryIdentifierType = null, string? PrimaryIdentifierValue = null);
public sealed record RetireDrugCommand(Guid DrugId);
public sealed record GetCodeSystemByKeyRequest(string SystemKey);
public sealed record GetActiveCodeSystemReleaseRequest(string SystemKey, DateOnly? AsOfDate = null);
public sealed record ResolveMedicalCodeRequest(string SystemKey, string Code, Guid? ReleaseId = null, DateOnly? AsOfDate = null);
public sealed record GetDrugRequest(Guid DrugId, DateOnly? AsOfDate = null);
public sealed record ResolveDrugIdentifierRequest(string IdentifierType, string IdentifierValue, DateOnly? AsOfDate = null);
public sealed record GetDrugProductRequest(Guid DrugProductId, DateOnly? AsOfDate = null);
public sealed record GetPackagedDrugRequest(Guid PackagedDrugId);
public sealed record GetMedicalServiceRequest(Guid ServiceId, DateOnly? AsOfDate = null);
public sealed record ResolveServiceCodeRequest(string SystemKey, string Code, Guid? ReleaseId = null, DateOnly? AsOfDate = null);
public sealed record GetServiceValuationRequest(Guid ServiceCodeId, int Year, string? Locality = null, string? FacilityIndicator = null, DateOnly? AsOfDate = null);
public sealed record GetCodeMapsRequest(Guid SourceCodeId, string? TargetSystemKey = null, string? MappingType = null, DateOnly? AsOfDate = null);
public sealed record ValidatePrescriptionDraftRequest(Guid PrescriptionId);
public sealed record PrescriptionEditItemRequest(MedicalPrescriptionItemCategory Category, string? ReferenceCode, string? DisplayName, string? DosageJson, int Quantity, int? DurationDays, string? MetadataJson = null);
public sealed record EditPrescriptionRequest(Guid PrescriptionId, int ExpectedVersion, IReadOnlyList<PrescriptionEditItemRequest> Items, string? IdempotencyKey = null, string? Reason = null);
public sealed record GetAvailableProvidersRequest;
public sealed record GetCredentialSetupGridRequest(Guid ClinicId, string? ProviderKey = null);
public sealed record TestCredentialRequest(Guid CredentialId);
public sealed record RevokeCredentialRequest(Guid CredentialId);
public sealed record ArchivePrescriptionTemplateRequest(Guid TemplateId, string TenantId);
public sealed record AddPrescriptionTemplateItemRequest(Guid TemplateId, string TenantId, MedicalPrescriptionItemCategory Category, string? ReferenceCode, string? DisplayName, int Quantity, int? DurationDays, string? DosageJson = null, string? MetadataJson = null);
public sealed record RemovePrescriptionTemplateItemRequest(Guid TemplateId, Guid ItemId, string TenantId);
public sealed record GetProviderTransactionMetricsRequest(string? ProviderKey = null, DateTime? From = null, DateTime? To = null, Guid? ClinicId = null);
public sealed record GetRejectionDistributionRequest(string? ProviderKey = null, DateTime? From = null, DateTime? To = null, Guid? ClinicId = null);
public sealed record GetCredentialHealthRequest(string? ProviderKey = null, DateTime? From = null, DateTime? To = null, Guid? ClinicId = null);
public sealed record GetDuplicateRiskIncidentsRequest(string? ProviderKey = null, DateTime? From = null, DateTime? To = null, Guid? ClinicId = null, int Take = 20);
public sealed record GetPrescriptionQuery { public required Guid PrescriptionId { get; init; } }
public sealed record GetPrescriptionHistoryQuery { public Guid? PatientId { get; init; } public Guid? ClinicId { get; init; } public MedicalPrescriptionStatus? StatusFilter { get; init; } public string? ProviderKeyFilter { get; init; } public DateTime? FromDate { get; init; } public DateTime? ToDate { get; init; } public int Skip { get; init; } public int Take { get; init; } = 20; }
public sealed record GetEligibilityQuery { public required string PatientNationalId { get; init; } public required string ProviderKey { get; init; } public required Guid DoctorId { get; init; } public required Guid ClinicId { get; init; } }
public sealed record GetAvailableProvidersQuery;
public sealed record CreateDraftCommand { public required Guid PatientId { get; init; } public string? PatientNationalId { get; init; } public required Guid DoctorId { get; init; } public required Guid ClinicId { get; init; } public Guid? EncounterId { get; init; } public string? SelectedProviderKey { get; init; } public string TenantId { get; init; } = string.Empty; }
public sealed record AddItemCommand { public required Guid PrescriptionId { get; init; } public required MedicalPrescriptionItemCategory Category { get; init; } public string? ReferenceCode { get; init; } public string? DisplayName { get; init; } public string? DosageJson { get; init; } public int Quantity { get; init; } = 1; public int? DurationDays { get; init; } public string? MetadataJson { get; init; } }
public sealed record RemoveItemCommand { public required Guid PrescriptionId { get; init; } public required Guid ItemId { get; init; } }
public sealed record ValidateDraftCommand { public required Guid PrescriptionId { get; init; } }
public sealed record SubmitPrescriptionCommand { public required Guid PrescriptionId { get; init; } public required string CorrelationId { get; init; } }
public sealed record EditPrescriptionCommand { public required Guid PrescriptionId { get; init; } public required string CorrelationId { get; init; } public string? EditReason { get; init; } }
public sealed record CancelPrescriptionCommand { public required Guid PrescriptionId { get; init; } public required string CorrelationId { get; init; } public string? Reason { get; init; } public string? ProviderCancellationWindowInfo { get; init; } }
public sealed record SetProviderCommand { public required Guid PrescriptionId { get; init; } public required string ProviderKey { get; init; } }
public sealed record RecordEligibilityCommand { public required Guid PrescriptionId { get; init; } public required MedicalEligibilityStatus Status { get; init; } public string? TrackingCode { get; init; } }
public sealed record PaperFallbackCommand { public required Guid PrescriptionId { get; init; } public required string Reason { get; init; } public string? PaperTrackingNumber { get; init; } public required string CorrelationId { get; init; } }
public sealed record ResolveDuplicateRiskCommand { public required Guid PrescriptionId { get; init; } public required DuplicateRiskResolution Resolution { get; init; } public string? Notes { get; init; } public required string CorrelationId { get; init; } }
public sealed record ManualReconcileCommand { public required Guid PrescriptionId { get; init; } public required string Notes { get; init; } public required string CorrelationId { get; init; } }
public sealed record RefreshProviderStatusCommand { public required Guid PrescriptionId { get; init; } public required string CorrelationId { get; init; } }
public sealed record ReconcilePaperFallbackCommand { public required Guid PrescriptionId { get; init; } public required string CorrelationId { get; init; } public string? RemotePrescriptionId { get; init; } }
public sealed record EscalateCommand { public required Guid PrescriptionId { get; init; } public required Guid OperatorId { get; init; } public required string Summary { get; init; } public required string CorrelationId { get; init; } }
public sealed record GetPrescriptionTemplatesQuery { public string TenantId { get; init; } = string.Empty; public bool IncludeArchived { get; init; } public string? SearchTerm { get; init; } }
public sealed record GetPrescriptionTemplateQuery { public Guid TemplateId { get; init; } public string TenantId { get; init; } = string.Empty; }
public sealed record CreatePrescriptionTemplateCommand { public string TenantId { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public string? Description { get; init; } }
public sealed record UpdatePrescriptionTemplateCommand { public Guid TemplateId { get; init; } public string TenantId { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public string? Description { get; init; } }
public sealed record ArchivePrescriptionTemplateCommand { public Guid TemplateId { get; init; } public string TenantId { get; init; } = string.Empty; }
public sealed record AddPrescriptionTemplateItemCommand { public Guid TemplateId { get; init; } public string TenantId { get; init; } = string.Empty; public MedicalPrescriptionItemCategory Category { get; init; } public string? ReferenceCode { get; init; } public string? DisplayName { get; init; } public string? DosageJson { get; init; } public int Quantity { get; init; } public int? DurationDays { get; init; } }
public sealed record RemovePrescriptionTemplateItemCommand { public Guid TemplateId { get; init; } public Guid ItemId { get; init; } public string TenantId { get; init; } = string.Empty; }
public sealed record LoadPrescriptionTemplateIntoDraftCommand { public Guid TemplateId { get; init; } public Guid PrescriptionId { get; init; } public string TenantId { get; init; } = string.Empty; }
public sealed record SaveCredentialCommand { public Guid? CredentialId { get; init; } public required Guid DoctorId { get; init; } public required string ProviderKey { get; init; } public required Guid ClinicId { get; init; } public string? ProviderDoctorIdentifier { get; init; } public string? ProviderUsername { get; init; } public string? ProviderClinicIdentifier { get; init; } public string? CredentialSecretRef { get; init; } }
public sealed record TestCredentialCommand { public required Guid CredentialId { get; init; } }
public sealed record RevokeCredentialCommand { public required Guid CredentialId { get; init; } }
public sealed record GetCredentialSetupGridQuery { public required Guid ClinicId { get; init; } public string? ProviderKey { get; init; } }
public sealed record SyncReferenceDataCommand { public required string ProviderKey { get; init; } public required string CodeSystem { get; init; } public string? TriggeredBy { get; init; } }
public sealed record GetProviderTransactionMetricsQuery { public string? ProviderKey { get; init; } public DateTime? From { get; init; } public DateTime? To { get; init; } }
public sealed record GetRejectionDistributionQuery { public string? ProviderKey { get; init; } public DateTime? From { get; init; } public DateTime? To { get; init; } }
public sealed record GetCredentialHealthQuery { public string? ProviderKey { get; init; } public Guid? ClinicId { get; init; } }
public sealed record GetOperationalAlertsQuery { public string? ProviderKey { get; init; } }
public sealed record GetDuplicateRiskIncidentsQuery { public string? ProviderKey { get; init; } public DateTime? From { get; init; } public DateTime? To { get; init; } public int Take { get; init; } = 20; }
public sealed record GetOutboxStatusQuery { public string? ProviderKey { get; init; } public string? TenantId { get; init; } }
public sealed record GetReconciliationQueueQuery { public string? TenantId { get; init; } public string? ProviderKey { get; init; } public MedicalPrescriptionStatus? StatusFilter { get; init; } public int Skip { get; init; } public int Take { get; init; } = 50; }
