namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagGroundedGenerationResult
{
    public required string Answer { get; init; }

    public required bool IsGrounded { get; init; }

    public required IReadOnlyList<RagGroundedCitation> Citations { get; init; }
}
