using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IClinicalAuditWriter
{
    Task WriteAsync(AuditWriteRequest request, CancellationToken cancellationToken = default);
}
