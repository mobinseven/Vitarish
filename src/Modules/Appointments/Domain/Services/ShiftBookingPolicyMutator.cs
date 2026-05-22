namespace Vitarish.Appointments.Domain.Services;

public static class ShiftBookingPolicyMutator
{
    public static ShiftBookingPolicy Create(Shift shift, UpsertShiftBookingPolicyCommandModel model)
    {
        ShiftBookingPolicy policy = new() { Id = model.PolicyId ?? Guid.NewGuid(), ShiftId = shift.Id };
        Apply(policy, model);
        shift.BookingPolicies.Add(policy);
        return policy;
    }

    public static void Update(ShiftBookingPolicy policy, UpsertShiftBookingPolicyCommandModel model) => Apply(policy, model);

    public static void Remove(Shift shift, ShiftBookingPolicy policy) => shift.BookingPolicies.Remove(policy);

    private static void Apply(ShiftBookingPolicy policy, UpsertShiftBookingPolicyCommandModel model)
    {
        policy.Date = model.Date;
        policy.BookingOpensBefore = model.BookingOpensBefore;
        policy.BookingCloseBefore = model.BookingCloseBefore;
        policy.Closed = model.Closed;
        policy.Capacity = model.Capacity;
        policy.RequiresApproval = model.RequiresApproval;
        policy.CancellationDeadline = model.CancellationDeadline;
        policy.EstimatedSessionLength = model.EstimatedSessionLength;
        policy.AllowRescheduling = model.AllowRescheduling;
        policy.MaxReservationsPerParty = model.MaxReservationsPerParty;
    }
}
