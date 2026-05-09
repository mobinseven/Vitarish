namespace Vitarish.Appointments.Domain.Models;

public record SessionDateRangeQuery
{
    public DateOnly DateFrom { get; init; }
    public DateOnly DateTo { get; init; }
}
