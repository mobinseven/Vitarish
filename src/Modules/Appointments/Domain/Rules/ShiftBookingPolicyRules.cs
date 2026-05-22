namespace Vitarish.Appointments.Domain.Rules;

public enum ShiftBookingPolicyErrorCode { ShiftNotFound, PolicyNotFound, InvalidDateForShift, DuplicateDate, InvalidCommand }

public sealed record ShiftBookingPolicyViolation(ShiftBookingPolicyErrorCode Code, string Field);

public static class ShiftBookingPolicyRules
{
    public static ShiftBookingPolicyViolation? ValidateWrite(Shift shift, DateOnly date, Guid? candidateId)
    {
        if (date.DayOfWeek != shift.DayOfWeek) return new(ShiftBookingPolicyErrorCode.InvalidDateForShift, nameof(date));
        bool duplicate = shift.BookingPolicies.Any(p => p.Date == date && p.Id != candidateId.GetValueOrDefault());
        return duplicate ? new ShiftBookingPolicyViolation(ShiftBookingPolicyErrorCode.DuplicateDate, nameof(date)) : null;
    }
}
