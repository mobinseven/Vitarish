namespace Vitarish.Financial.Domain.Models;

public static class FinancialPostingRules
{
    public static void ValidateOwnerScope(Guid? tenantId, LedgerOwnerScope ownerScope)
    {
        if (ownerScope == LedgerOwnerScope.Tenant && tenantId is null)
        {
            throw new InvalidOperationException("Tenant owner scope requires a tenant id.");
        }

        if (ownerScope == LedgerOwnerScope.Platform && tenantId is not null)
        {
            throw new InvalidOperationException("Platform owner scope must not have a tenant id.");
        }
    }

    public static long GetBalanceDelta(LedgerAccountClass accountClass, LedgerEntrySide side, long amount)
    {
        bool debitIncreases = accountClass is LedgerAccountClass.Asset or LedgerAccountClass.Expense;
        bool isDebit = side == LedgerEntrySide.Debit;
        bool increase = debitIncreases ? isDebit : !isDebit;

        return increase ? amount : -amount;
    }

    public static void ValidateBalanced(IReadOnlyCollection<LedgerEntryLine> lines)
    {
        long debits = lines.Where(l => l.Side == LedgerEntrySide.Debit).Sum(l => l.AmountMinorUnits);
        long credits = lines.Where(l => l.Side == LedgerEntrySide.Credit).Sum(l => l.AmountMinorUnits);

        if (debits <= 0 || credits <= 0 || debits != credits)
        {
            throw new InvalidOperationException("Entry lines must be balanced with positive debit and credit totals.");
        }
    }
}
