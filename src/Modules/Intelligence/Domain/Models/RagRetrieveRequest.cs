namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagRetrieveRequest
{
    public required string Query { get; init; }

    public int MaxResults { get; init; } = 5;
}
