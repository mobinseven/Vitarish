namespace Vitarish.Intelligence.Domain.Options;

public sealed class AgentRuntimeOptions
{
    /// <summary>
    /// The model the agent runtime's provider serves. Agents fall back to
    /// <see cref="AiOptions.DefaultResponseModel"/> when it is not set.
    /// </summary>
    public const string ModelConfigurationKey = "Vitarish:Intelligence:AgentRuntime:Model";

    public string BaseUrl { get; set; } = "http://localhost:3003";
    public string ToolCallbackUrl { get; set; } = "http://localhost:5000/api/modules/intelligence/internal/agent/tools/execute";
    public string SharedSecret { get; set; } = string.Empty;
}
