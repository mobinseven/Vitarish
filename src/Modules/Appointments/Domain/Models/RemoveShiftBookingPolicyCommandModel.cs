namespace Vitarish.Appointments.Domain.Models;

public record RemoveShiftBookingPolicyCommandModel
{
    public Guid ShiftId { get; init; }
    public Guid PolicyId { get; init; }
}
