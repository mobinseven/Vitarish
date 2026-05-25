namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagPlatformAdminService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
