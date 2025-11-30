using Vorn.Ai.Server.Domain.Models;

namespace Vorn.Ai.Server.Infrastructure.Interfaces;

public interface IResponder
{
    Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default);
    IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default);
}
