namespace Vitarish.Calendar.Domain.Dto;

public record AvailabilityPolicyWriteDto : EntityWriteDto
{
    public Guid CalendarAccountId { get; set; }
    public CalendarObjectCategory Category { get; set; }
    public AvailabilityEffect Effect { get; set; }
}
