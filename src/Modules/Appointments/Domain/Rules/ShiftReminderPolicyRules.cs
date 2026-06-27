using Vitarish.Appointments.Domain.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Domain.Rules;

public static class ShiftReminderPolicyRules
{
    public static EntityResult Validate(ShiftReminderPolicyWriteDto dto)
    {
        if (dto.OffsetBefore <= TimeSpan.Zero)
        {
            return EntityResult.Failure(EntityError.Validation("ShiftReminderPolicyOffsetBeforePositiveError", nameof(dto.OffsetBefore)));
        }

        return dto.QualifyingStatuses is not { Count: > 0 }
            ? EntityResult.Failure(EntityError.Validation("ShiftReminderPolicyQualifyingStatusesRequiredError", nameof(dto.QualifyingStatuses)))
            : string.IsNullOrWhiteSpace(dto.SubjectTemplate)
            ? EntityResult.Failure(EntityError.Validation("ShiftReminderPolicySubjectTemplateRequiredError", nameof(dto.SubjectTemplate)))
            : string.IsNullOrWhiteSpace(dto.BodyTemplate)
            ? EntityResult.Failure(EntityError.Validation("ShiftReminderPolicyBodyTemplateRequiredError", nameof(dto.BodyTemplate)))
            : EntityResult.Success();
    }
}
