namespace Vitarish.Intelligence.Domain.Models;

public sealed record RagHealthReport
{
    public required bool IsHealthy { get; init; }

    public required DateTimeOffset CheckedAt { get; init; }

    public required string Details { get; init; }
}
