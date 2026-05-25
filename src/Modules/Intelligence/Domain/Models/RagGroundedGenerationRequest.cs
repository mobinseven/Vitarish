namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagGroundedGenerationRequest
{
    public required string Query { get; init; }

    public int MaxDocuments { get; init; } = 3;

    public string? Model { get; init; }

    public string? Instructions { get; init; }
}
