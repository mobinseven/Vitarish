namespace Vitarish.Financial.Application.Contracts;

public sealed record PostLedgerEntryCommand(
    Guid? TenantId,
    string IdempotencyKey,
    string Currency,
    int Exponent,
    PostingTemplateId TemplateId,
    LedgerReasonCode ReasonCode,
    FinancialMutationMetadata Metadata,
    IReadOnlyList<PostLedgerLineRequest> Lines);
