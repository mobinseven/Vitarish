namespace Vitarish.Appointments.Domain.Rules;

public enum ShiftBookingPolicyErrorCode { ShiftNotFound, PolicyNotFound, InvalidDateForShift, DuplicateDate, InvalidCommand }

public sealed record ShiftBookingPolicyViolation(ShiftBookingPolicyErrorCode Code, string Field);

public static class ShiftBookingPolicyRules
{
    public static ShiftBookingPolicyViolation? ValidateWrite(DayOfWeek shiftDayOfWeek, DateOnly date, IEnumerable<ShiftBookingPolicy> existingPolicies, Guid? candidateId)
    {
        if (date.DayOfWeek != shiftDayOfWeek)
        {
            return new(ShiftBookingPolicyErrorCode.InvalidDateForShift, nameof(date));
        }

        bool duplicate = existingPolicies.Any(p => p.Date == date && p.Id != candidateId.GetValueOrDefault());
        return duplicate ? new ShiftBookingPolicyViolation(ShiftBookingPolicyErrorCode.DuplicateDate, nameof(date)) : null;
    }
}
