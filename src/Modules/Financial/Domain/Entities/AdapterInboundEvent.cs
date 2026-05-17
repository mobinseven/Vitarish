namespace Vitarish.Financial.Domain.Entities;

public sealed class AdapterInboundEvent : Entity
{
    public required string Provider { get; set; }

    public required string EventId { get; set; }

    public required string DedupeKey { get; set; }

    public required string SchemaVersion { get; set; }

    public required string RawPayload { get; set; }

    public required string NormalizedPayload { get; set; }

    public Guid? PostedEntryId { get; set; }
}
