namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagRerankingService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
