using OpenAI.Responses;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

public sealed class OpenAiToolSchemaMapper
{
#pragma warning disable OPENAI001
    internal FunctionTool Map(AgentToolDefinition tool) => ResponseTool.CreateFunctionTool(
        tool.Name,
        BinaryData.FromString(tool.ParametersJsonSchema),
        strictModeEnabled: true,
        tool.Description);
#pragma warning restore OPENAI001
}
