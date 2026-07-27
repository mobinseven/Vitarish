using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

internal sealed record OpenAiAgentRequest(
    AgentDefinition Definition,
    string? UserInput,
    IReadOnlyList<AgentToolDefinition> Tools,
    IReadOnlyList<AgentToolResult> ToolResults,
    string? PreviousResponseId,
    int MaxOutputTokens);

internal sealed record OpenAiAgentResponse(
    string Id,
    string? Output,
    IReadOnlyList<AgentToolCall> ToolCalls,
    int InputTokens,
    int OutputTokens);

internal interface IOpenAiAgentClient
{
    Task<OpenAiAgentResponse> CreateResponseAsync(
        OpenAiAgentRequest request,
        CancellationToken cancellationToken);
}
