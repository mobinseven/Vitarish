using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Dto;

public record CalendarViewDto
{
    public TimeRangeQuery Range { get; init; } = new();
    public IReadOnlyList<CalendarEventViewDto> Events { get; init; } = [];
}
