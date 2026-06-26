namespace Vitarish.Appointments.Contracts.Models;

public record ShiftDateRangeQuery
{
    public DateOnly DateFrom { get; init; }
    public DateOnly DateTo { get; init; }
}
