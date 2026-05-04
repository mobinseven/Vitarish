namespace Vitarish.Calendar.Domain.Models;

public record TimeWindow
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}
