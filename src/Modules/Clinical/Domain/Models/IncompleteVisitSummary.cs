namespace Vitarish.Clinical.Domain.Models;

public sealed record IncompleteVisitSummary
{
    public int TotalOpen { get; init; }
    public int TotalInTriage { get; init; }
    public int TotalReadyForProvider { get; init; }
    public int TotalInProgress { get; init; }
    public int TotalSigned { get; init; }
    public int TotalStale { get; init; }
}
