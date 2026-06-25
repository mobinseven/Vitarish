namespace Vitarish.Communications.Contracts.Errors;
public enum CommunicationsErrorCode { None, ValidationFailure, Unauthorized, Forbidden, NotFound, Duplicate, ProviderUnavailable, ProviderRejected, Timeout, Cancellation, DeserializationFailure, TransportFailure, UnexpectedFailure }
public sealed record CommunicationsError(CommunicationsErrorCode Code, string Message);
public sealed record ProviderFailurePayload(string Provider, string? FailureCode, string Message, bool IsTransient);
