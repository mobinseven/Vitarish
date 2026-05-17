namespace Vitarish.Financial.Application.Abstractions;

public interface IFinancialLedgerCommandService
{
    Task<FinancialCommandResult<PostLedgerEntryResponse>> PostAsync(PostLedgerEntryCommand command, CancellationToken cancellationToken);

    Task<FinancialCommandResult<PostLedgerEntryResponse>> ReverseAsync(ReverseLedgerEntryCommand command, CancellationToken cancellationToken);

    Task<FinancialCommandResult<Guid>> RecordMismatchAsync(ReconciliationMismatchCommand command, CancellationToken cancellationToken);
}
