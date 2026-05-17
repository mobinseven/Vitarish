namespace Vitarish.Financial.Infrastructure.Services;

[RegisterTransient]
internal sealed class FinancialAdapterIngressService(
    FinancialDbContext dbContext,
    IFinancialLedgerCommandService commandService)
    : IFinancialAdapterIngressService
{
    public async Task<FinancialCommandResult<PostLedgerEntryResponse>> IngestAsync(AdapterIngressCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Provider)
            || string.IsNullOrWhiteSpace(command.EventId)
            || string.IsNullOrWhiteSpace(command.DedupeKey)
            || string.IsNullOrWhiteSpace(command.SchemaVersion)
            || string.IsNullOrWhiteSpace(command.RawPayload)
            || string.IsNullOrWhiteSpace(command.NormalizedPayload))
        {
            return FinancialCommandResult<PostLedgerEntryResponse>.Validation("Provider event payload fields are required.");
        }

        AdapterInboundEvent? existing = await dbContext.AdapterInboundEvents
            .SingleOrDefaultAsync(e => e.Provider == command.Provider && e.DedupeKey == command.DedupeKey, cancellationToken);

        if (existing is not null)
        {
            if (existing.PostedEntryId is { } postedEntryId)
            {
                LedgerEntry? entry = await dbContext.LedgerEntries.SingleOrDefaultAsync(e => e.Id == postedEntryId, cancellationToken);
                if (entry is not null)
                {
                    return FinancialCommandResult<PostLedgerEntryResponse>.Ok(
                        new PostLedgerEntryResponse(entry.Id, entry.State, entry.PostedAt, command.PostCommand.IdempotencyKey),
                        isReplay: true);
                }
            }

            return FinancialCommandResult<PostLedgerEntryResponse>.Conflict("Duplicate adapter event dedupe key.");
        }

        AdapterInboundEvent inbound = new()
        {
            Id = Guid.CreateVersion7(),
            Provider = command.Provider.Trim(),
            EventId = command.EventId.Trim(),
            DedupeKey = command.DedupeKey.Trim(),
            SchemaVersion = command.SchemaVersion.Trim(),
            RawPayload = command.RawPayload,
            NormalizedPayload = command.NormalizedPayload
        };

        _ = await dbContext.AdapterInboundEvents.AddAsync(inbound, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        FinancialCommandResult<PostLedgerEntryResponse> posted = await commandService.PostAsync(command.PostCommand, cancellationToken);
        if (posted.Success && posted.Value is not null)
        {
            inbound.PostedEntryId = posted.Value.EntryId;
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return posted;
    }
}
