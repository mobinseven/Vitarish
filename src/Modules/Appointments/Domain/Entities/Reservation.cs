namespace Vitarish.Appointments.Domain.Entities;

public class Reservation : Entity
{
    public ReservationStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    } = ReservationStatus.Pending;

    public Guid SessionOccurrenceId
    {
        get;
        set => SetField(ref field, value, nameof(SessionOccurrenceId));
    }

    public string? ReservingPartyId
    {
        get;
        set => SetField(ref field, value, nameof(ReservingPartyId));
    }

    public DateTimeOffset? ConfirmedAt
    {
        get;
        set => SetField(ref field, value, nameof(ConfirmedAt));
    }

    public DateTimeOffset? CancelledAt
    {
        get;
        set => SetField(ref field, value, nameof(CancelledAt));
    }

    public bool IsHold
    {
        get;
        set => SetField(ref field, value, nameof(IsHold));
    }

    public DateTimeOffset? ExpiresAt
    {
        get;
        set => SetField(ref field, value, nameof(ExpiresAt));
    }

    public SessionOccurrence? Occurrence { get; set; }
}
