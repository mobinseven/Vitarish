using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Sea.Services;

public interface IMcpToolProvider
{
    Task<IReadOnlyList<ResponderToolDefinition>> GetDefinitionsAsync(
        string userId,
        IEnumerable<string>? pluginIdentifiers,
        bool includeCommands = true,
        CancellationToken cancellationToken = default);
}
