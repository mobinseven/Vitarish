namespace Vitarish.Intelligence.Domain.Options;

public sealed class AgentRuntimeOptions
{
    public string BaseUrl { get; set; } = "http://localhost:3003";
    public string ToolCallbackUrl { get; set; } = "http://localhost:5000/api/modules/intelligence/internal/agent/tools/execute";
    public string SharedSecret { get; set; } = string.Empty;
}
