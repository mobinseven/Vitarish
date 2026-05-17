namespace Vitarish.Financial.Application.Abstractions;

public interface IFinancialLedgerQueryService
{
    Task<long?> GetBalanceMinorUnitsAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, long>> RebuildBalancesFromPostedHistoryAsync(CancellationToken cancellationToken);
}
