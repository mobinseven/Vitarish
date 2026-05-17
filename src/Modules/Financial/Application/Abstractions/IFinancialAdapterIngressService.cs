namespace Vitarish.Financial.Application.Abstractions;

public interface IFinancialAdapterIngressService
{
    Task<FinancialCommandResult<PostLedgerEntryResponse>> IngestAsync(AdapterIngressCommand command, CancellationToken cancellationToken);
}
