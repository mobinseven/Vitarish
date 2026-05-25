namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagRetrievedDocument
{
    public required string Id { get; init; }

    public required string Content { get; init; }

    public required string Source { get; init; }

    public double Score { get; init; }
}
