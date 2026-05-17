namespace Vitarish.Financial.Application.Contracts;

public sealed record AdapterIngressCommand(
    string Provider,
    string EventId,
    string DedupeKey,
    string SchemaVersion,
    string RawPayload,
    string NormalizedPayload,
    PostLedgerEntryCommand PostCommand);
