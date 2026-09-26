namespace Vitarish.Audit.Domain;

public sealed record SecurityAuditEventMetadata
{
    public string? Identifier { get; init; }
}
