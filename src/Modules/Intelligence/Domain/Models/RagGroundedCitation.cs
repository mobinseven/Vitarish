namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagGroundedCitation
{
    public required string DocumentId { get; init; }

    public required string Source { get; init; }

    public double Score { get; init; }
}
