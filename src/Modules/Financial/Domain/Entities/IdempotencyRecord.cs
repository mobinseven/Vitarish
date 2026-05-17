namespace Vitarish.Financial.Domain.Entities;

public sealed class IdempotencyRecord : Entity
{
    public required string Scope { get; set; }

    public required string IdempotencyKey { get; set; }

    public required string RequestHash { get; set; }

    public string? ResponsePayload { get; set; }

    public bool IsCompleted { get; set; }
}
