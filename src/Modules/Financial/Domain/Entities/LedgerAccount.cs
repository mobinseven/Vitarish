namespace Vitarish.Financial.Domain.Entities;

public sealed class LedgerAccount : Entity
{
    public required string Key { get; set; }

    public Guid? TenantId { get; set; }

    public LedgerOwnerScope OwnerScope { get; set; }

    public LedgerAccountClass AccountClass { get; set; }

    public required string Currency { get; set; }

    public int Exponent { get; set; }

    public long BalanceMinorUnits { get; set; }

    public long Sequence { get; set; }

    public void ValidateOwnership() => FinancialPostingRules.ValidateOwnerScope(TenantId, OwnerScope);
}
