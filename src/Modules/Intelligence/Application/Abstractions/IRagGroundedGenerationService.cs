using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagGroundedGenerationService
{
    Task<RagGroundedGenerationResult> GenerateAsync(
        RagGroundedGenerationRequest request,
        CancellationToken cancellationToken = default);
}
