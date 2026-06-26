namespace Vitarish.Medical.Contracts.Providers;

public sealed record MedicalProviderOperationMetadata(string? ProviderKey, string OperationName, string? CorrelationId, int RetryCount = 0, int MaxRetries = 0, DateTime? NextRetryAt = null, string? UserSafeMessage = null, string? ErrorCode = null);
public sealed record ProviderSyncResult(MedicalProviderOperationMetadata Metadata, bool IsSuccess);
public sealed record ReferenceDataSyncResult(MedicalProviderOperationMetadata Metadata, bool IsSuccess, int RecordsProcessed = 0, int RecordsUpdated = 0);
public sealed record CredentialValidationResult(MedicalProviderOperationMetadata Metadata, bool IsValid);
public sealed record CredentialConfigurationResult(MedicalProviderOperationMetadata Metadata, Guid? CredentialId, bool IsConfigured);
public sealed record ProviderOutageResult(MedicalProviderOperationMetadata Metadata, DateTime? EstimatedRecoveryAt = null);
public sealed record RetryExhaustionResult(MedicalProviderOperationMetadata Metadata);
public sealed record ReconciliationResult(MedicalProviderOperationMetadata Metadata, bool IsReconciled);
public sealed record RecoveryResult(MedicalProviderOperationMetadata Metadata, bool IsRecovered);
