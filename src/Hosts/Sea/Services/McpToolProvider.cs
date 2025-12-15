using Vitarish.Clients.Interface.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Constants;
using Vitarish.Clients.Interface.Sea.Plugins;
using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Sea.Services;

public interface IMcpToolProvider
{
    Task<IReadOnlyList<ResponderToolDefinition>> GetDefinitionsAsync(
        IEnumerable<string>? pluginIdentifiers,
        bool includeCommands = true,
        CancellationToken cancellationToken = default);
}

public sealed class McpToolProvider(
    IHttpContextAccessor httpContextAccessor,
    IPersonalAccessTokenClient accessTokenClient) : IMcpToolProvider
{
    private static readonly IReadOnlyList<string> BaseQueryTools = McpToolDefinitions.QueryToolNames;
    private static readonly IReadOnlyList<string> BaseCommandTools = McpToolDefinitions.CommandToolNames;

    public async Task<IReadOnlyList<ResponderToolDefinition>> GetDefinitionsAsync(
        IEnumerable<string>? pluginIdentifiers,
        bool includeCommands = true,
        CancellationToken cancellationToken = default)
    {
        string? accessToken = await accessTokenClient.CreateTokenAsync(scopes: [IdentityConstants.AccessScope], cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Unable to retrieve an access token for the configured API client.");
        }

        List<string> queryTools = [.. BaseQueryTools];
        List<string> commandTools = [.. BaseCommandTools];
        HashSet<string> querySet = new(BaseQueryTools, StringComparer.Ordinal);
        HashSet<string> commandSet = new(BaseCommandTools, StringComparer.Ordinal);

        string hostUri = httpContextAccessor.HttpContext?.Request.Host.Host ?? throw new InvalidOperationException("HttpContext can not be null.");
        string serverUrl = $"https://{hostUri}/mcp";

        ResponderToolDefinition queries = new()
        {
            Type = "mcp",
            ServerLabel = $"{hostUri}-queries",
            ServerUrl = serverUrl,
            ServerToken = accessToken,
            AllowedTools = [.. queryTools],
            RequireApproval = ResponderToolApprovalRequirement.Never
        };
        if (includeCommands == false)
        {
            return [queries];
        }

        ResponderToolDefinition commands = new()
        {
            Type = "mcp",
            ServerLabel = $"{hostUri}-commands",
            ServerUrl = serverUrl,
            ServerToken = accessToken,
            AllowedTools = [.. commandTools],
            RequireApproval = ResponderToolApprovalRequirement.Always
        };

        return [queries, commands];
    }
}
