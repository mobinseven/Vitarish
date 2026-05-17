namespace Vitarish.Financial.Application.Contracts;

public sealed record ReverseLedgerEntryCommand(
    Guid EntryId,
    string IdempotencyKey,
    FinancialMutationMetadata Metadata,
    PostingTemplateId TemplateId = PostingTemplateId.ManualAdjustment,
    LedgerReasonCode ReasonCode = LedgerReasonCode.Reverse);
