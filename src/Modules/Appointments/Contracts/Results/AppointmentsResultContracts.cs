namespace Vitarish.Appointments.Contracts.Results;

public enum AppointmentsErrorCode
{
    None, HoldAccepted, HoldExpired, SlotUnavailable, BookingConflict, ValidationFailure, Unauthorized, Forbidden, NotFound, CancellationRejected, RequestCancellation, StaleState, Timeout, TransportFailure, DeserializationFailure, UnexpectedFailure
}

public sealed record AppointmentsError(AppointmentsErrorCode Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
public sealed record AppointmentsCommandResult(bool Succeeded, Guid? EntityId = null, AppointmentsError? Error = null);
public sealed record AppointmentsQueryResult<T>(T? Value, AppointmentsError? Error = null)
{
    public bool Succeeded => Error is null;
}
public sealed record TemporaryHoldAcceptedResponse(Guid HoldId, DateTimeOffset ExpiresAtUtc);
public sealed record TemporaryHoldStateDto(Guid HoldId, Guid ShiftId, DateOnly Date, TimeOnly? StartTime, DateTimeOffset ExpiresAtUtc, bool IsConfirmed, bool IsExpired);
