namespace Vitarish.Calendar.Domain.Models;

public record TimeRangeQuery
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public string TimeZone { get; init; } = "UTC";
}
