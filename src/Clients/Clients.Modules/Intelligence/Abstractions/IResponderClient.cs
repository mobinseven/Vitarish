using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Modules.Intelligence.Abstractions;

public interface IResponderClient
{
    Task<AvailableModels> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ResponderOutput> StreamAsync(IEnumerable<ResponderInput> input, ResponderOptions options, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ResponderOutput> StreamRewriteAsync(string prompt, CancellationToken cancellationToken = default);
}
