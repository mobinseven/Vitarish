namespace Vitarish.Audit.Application;

public interface IAuditStorageSizeReader
{
    Task<long> GetStorageSizeBytesAsync(CancellationToken cancellationToken);
}
