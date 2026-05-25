namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagGroundedGenerationService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
