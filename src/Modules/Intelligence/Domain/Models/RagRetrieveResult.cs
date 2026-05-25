namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagRetrieveResult
{
    public required IReadOnlyList<RagRetrievedDocument> Documents { get; init; }
}
