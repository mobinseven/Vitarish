using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Appointments.Domain.Rules;

public static class ShiftReminderPolicyRules
{
    public static EntityResult Validate(ShiftReminderPolicyWriteDto dto)
    {
        if (dto.OffsetBefore <= TimeSpan.Zero)
            return EntityResult.Failure(EntityError.Validation("ShiftReminderPolicyOffsetBeforePositiveError", nameof(dto.OffsetBefore)));

        if (dto.QualifyingStatuses is not { Count: > 0 })
            return EntityResult.Failure(EntityError.Validation("ShiftReminderPolicyQualifyingStatusesRequiredError", nameof(dto.QualifyingStatuses)));

        if (string.IsNullOrWhiteSpace(dto.SubjectTemplate))
            return EntityResult.Failure(EntityError.Validation("ShiftReminderPolicySubjectTemplateRequiredError", nameof(dto.SubjectTemplate)));

        if (string.IsNullOrWhiteSpace(dto.BodyTemplate))
            return EntityResult.Failure(EntityError.Validation("ShiftReminderPolicyBodyTemplateRequiredError", nameof(dto.BodyTemplate)));

        return EntityResult.Success();
    }
}
