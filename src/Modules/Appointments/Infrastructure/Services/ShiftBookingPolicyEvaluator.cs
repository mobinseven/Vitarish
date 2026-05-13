namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class ShiftBookingPolicyEvaluator
{
    public bool IsBookingOpen(Shift shift, DateOnly date)
        => ShiftBookingRules.IsBookingOpen(shift, date, DateTimeOffset.UtcNow);

    public bool CanCancel(Shift shift, Reservation reservation)
    {
        TimeSpan? deadline = shift.BookingPolicy?.CancellationDeadline;
        if (deadline is null)
        {
            return true;
        }

        DateTimeOffset cutoff = ToDateTimeOffset(reservation.Date, shift.StartTime) - deadline.Value;
        return DateTimeOffset.UtcNow < cutoff;
    }

    public bool CanReschedule(Shift shift)
        => shift.BookingPolicy?.AllowRescheduling ?? false;

    public bool RequiresApproval(Shift shift)
        => shift.BookingPolicy?.RequiresApproval ?? false;

    public bool NeedsPartyCapacityCheck(Shift shift, string? reservingPartyId)
        => !string.IsNullOrWhiteSpace(reservingPartyId)
            && shift.BookingPolicy?.MaxReservationsPerParty is not null;

    public bool HasCapacityForParty(Shift shift, string? reservingPartyId, int currentReservationCount)
    {
        if (!NeedsPartyCapacityCheck(shift, reservingPartyId))
        {
            return true;
        }

        return currentReservationCount < shift.BookingPolicy!.MaxReservationsPerParty!.Value;
    }

    private static DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
