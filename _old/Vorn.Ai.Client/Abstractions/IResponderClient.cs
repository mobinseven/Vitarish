using Vorn.Ai.Client.Models;

namespace Vorn.Ai.Client.Abstractions;

public interface IResponderClient
{
    Task<AvailableModels> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ResponderOutput> StreamAsync(IEnumerable<ResponderInput> input, ResponderOptions options, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ResponderOutput> StreamRewriteAsync(string prompt, CancellationToken cancellationToken = default);
}
