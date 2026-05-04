namespace Vitarish.Calendar.Domain.Models;

public record RecurrenceInfo
{
    public string RRule { get; init; } = string.Empty;
    public IReadOnlyList<DateTimeOffset> ExDates { get; init; } = [];
    public IReadOnlyList<DateTimeOffset> RDates { get; init; } = [];
    public bool IsRecurring { get; init; }
}
