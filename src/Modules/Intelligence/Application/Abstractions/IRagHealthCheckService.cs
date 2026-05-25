using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagHealthCheckService
{
    Task<RagHealthReport> CheckAsync(CancellationToken cancellationToken = default);
}
