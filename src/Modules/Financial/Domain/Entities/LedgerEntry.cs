namespace Vitarish.Financial.Domain.Entities;

public sealed class LedgerEntry : Entity
{
    public LedgerEntryState State { get; set; } = LedgerEntryState.Draft;

    public Guid? TenantId { get; set; }

    public required string Currency { get; set; }

    public int Exponent { get; set; }

    public PostingTemplateId TemplateId { get; set; }

    public LedgerReasonCode ReasonCode { get; set; }

    public required string Actor { get; set; }

    public required string Source { get; set; }

    public required string CorrelationId { get; set; }

    public required string CausationId { get; set; }

    public string? ProviderReference { get; set; }

    public Guid? ReversalOfEntryId { get; set; }

    public DateTimeOffset? PostedAt { get; set; }

    public List<LedgerEntryLine> Lines { get; set; } = [];

    public void Post(DateTimeOffset postedAt)
    {
        if (State != LedgerEntryState.Draft)
        {
            throw new InvalidOperationException("Only draft entries can be posted.");
        }

        FinancialPostingRules.ValidateBalanced(Lines);

        State = LedgerEntryState.Posted;
        PostedAt = postedAt;
    }

    public void VoidDraft()
    {
        if (State != LedgerEntryState.Draft)
        {
            throw new InvalidOperationException("Only draft entries can be voided.");
        }

        State = LedgerEntryState.Voided;
    }
}
