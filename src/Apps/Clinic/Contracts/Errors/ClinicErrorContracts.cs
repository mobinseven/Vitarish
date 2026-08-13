namespace Vitarish.Clinic.Contracts.Errors;
public enum ClinicErrorCode { ValidationFailure, Unauthorized, Forbidden, NotFound, Conflict, InvalidHistory, Timeout, Cancellation, TransportFailure, DeserializationFailure, UnexpectedFailure }
public sealed record ClinicError(ClinicErrorCode Code, string Message, IReadOnlyDictionary<string,string[]>? ValidationErrors = null);
