using OpenAI.Responses;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

public sealed class OpenAiAgentRequestMapper(OpenAiToolSchemaMapper toolSchemaMapper)
{
#pragma warning disable OPENAI001
    internal CreateResponseOptions Map(OpenAiAgentRequest request)
    {
        List<ResponseItem> input = [];

        if (request.UserInput is not null)
        {
            input.Add(ResponseItem.CreateUserMessageItem(request.UserInput));
        }

        input.AddRange(request.ToolResults.Select(result =>
            ResponseItem.CreateFunctionCallOutputItem(result.CallId, result.Output)));

        CreateResponseOptions options = new(request.Definition.Model, input)
        {
            Instructions = request.Definition.Instructions,
            PreviousResponseId = request.PreviousResponseId,
            MaxOutputTokenCount = request.MaxOutputTokens,
            ParallelToolCallsEnabled = false
        };

        foreach (Application.Agents.AgentToolDefinition tool in request.Tools)
        {
            options.Tools.Add(toolSchemaMapper.Map(tool));
        }

        if (request.Definition.Result is { } result)
        {
            options.TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    result.Name,
                    BinaryData.FromString(result.JsonSchema),
                    jsonSchemaIsStrict: result.Strict)
            };
        }

        return options;
    }
#pragma warning restore OPENAI001
}
