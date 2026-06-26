namespace Vitarish.Medical.Contracts.Errors;

public enum MedicalErrorCode { ValidationFailure, Unauthorized, Forbidden, NotFound, Conflict, ProviderUnavailable, ProviderRejected, CredentialInvalid, CredentialExpired, CredentialRevoked, SyncFailure, RetryExhaustion, ReconciliationFailure, Timeout, Cancellation, DeserializationFailure, UnexpectedFailure }
public sealed record MedicalError(MedicalErrorCode Code, string Message, int? StatusCode = null, string? CorrelationId = null, IReadOnlyDictionary<string,string[]>? ValidationErrors = null);
