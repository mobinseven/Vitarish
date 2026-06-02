using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Extensions;
using DomainResponseTextFormat = Vitarish.Intelligence.Domain.Models.ResponseTextFormat;
using OpenAiResponseTextFormat = OpenAI.Responses.ResponseTextFormat;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi;

public sealed class OpenAiResponder(IOptionsMonitor<AiOptions> aiOptions) : IResponder
{
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable OPENAICUA001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

    public async Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        (OpenAIResponseClient client, ResponseCreationOptions creation) = await CreateResponseClient(options);

        IReadOnlyList<ResponseItem> items = [.. ResponderHelpers.MapInput(input)];

        OpenAIResponse response = await client.CreateResponseAsync(items, creation, ct);

        IReadOnlyList<ResponderOutput> mapped = ResponderHelpers.MapResponse(response);

        return mapped;
    }

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        (OpenAIResponseClient client, ResponseCreationOptions creation) = await CreateResponseClient(options);

        IReadOnlyList<ResponseItem> items = [.. ResponderHelpers.MapInput(input)];

        System.ClientModel.AsyncCollectionResult<StreamingResponseUpdate> updates = client.CreateResponseStreamingAsync(items, creation, ct);

        await foreach (StreamingResponseUpdate? update in updates.WithCancellation(ct))
        {
            switch (update)
            {
                case StreamingResponseOutputTextDeltaUpdate textDelta:
                    yield return ResponderOutput.TextOutput(textDelta.Delta);
                    break;
                case StreamingResponseCompletedUpdate completed:
                    foreach (ResponderOutput? item in ResponderHelpers.MapResponse(completed.Response).Where(r => r.Type != ResponderOutputType.Text))
                    {
                        yield return item;
                    }
                    break;
            }
        }
    }

    private Task<(OpenAIResponseClient client, ResponseCreationOptions creation)> CreateResponseClient(ResponderOptions options)
    {
        AiOptions optionsValue = aiOptions.CurrentValue;

        string model = string.IsNullOrWhiteSpace(options.Model)
            ? optionsValue.DefaultResponseModel
            : options.Model!.Trim();

        OpenAIResponseClient client = new(
            model: model,
            apiKey: optionsValue.OpenAi.ApiKey);

        ResponseCreationOptions creation = new();

        if (!string.IsNullOrWhiteSpace(options.Instructions))
        {
            creation.Instructions = options.Instructions;
        }

        if (!string.IsNullOrWhiteSpace(options.PreviousResponseId))
        {
            creation.PreviousResponseId = options.PreviousResponseId;
        }

        ResponseTextOptions textOptions = new()
        {
            TextFormat = options.TextFormat switch
            {
                DomainResponseTextFormat.JsonObject => OpenAiResponseTextFormat.CreateJsonObjectFormat(),
                _ => OpenAiResponseTextFormat.CreateTextFormat()
            }
        };

        creation.TextOptions = textOptions;

        foreach (ResponderToolDefinition tool in options.Tools ?? Array.Empty<ResponderToolDefinition>())
        {
            if (!string.Equals(tool.Type, "mcp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(tool.ServerLabel) || string.IsNullOrWhiteSpace(tool.ServerUrl))
            {
                continue;
            }

            if (!Uri.TryCreate(tool.ServerUrl, UriKind.Absolute, out Uri? serverUri))
            {
                continue;
            }

            Dictionary<string, string>? headers = string.IsNullOrWhiteSpace(tool.ServerToken)
                ? null
                : new Dictionary<string, string> { ["Authorization"] = $"Bearer {tool.ServerToken}" };

            McpToolFilter? allowedTools = null;

            if (tool.AllowedTools is { Count: > 0 } allowedList)
            {
                allowedTools = (McpToolFilter?)Activator.CreateInstance(
                    typeof(McpToolFilter),
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    args: new object?[] { allowedList.ToList(), new Dictionary<string, System.BinaryData>() },
                    culture: null);
            }

            GlobalMcpToolCallApprovalPolicy policy = tool.RequireApproval switch
            {
                ResponderToolApprovalRequirement.Always => GlobalMcpToolCallApprovalPolicy.AlwaysRequireApproval,
                _ => GlobalMcpToolCallApprovalPolicy.NeverRequireApproval
            };

            McpToolCallApprovalPolicy approvalPolicy = new(policy);

            ResponseTool mcpTool = ResponseTool.CreateMcpTool(
                serverLabel: tool.ServerLabel,
                serverUri: serverUri,
                headers: headers,
                allowedTools: allowedTools,
                toolCallApprovalPolicy: approvalPolicy);

            creation.Tools.Add(mcpTool);
        }
        //creation.Tools.Add(ResponseTool.CreateFunctionTool(
        //    functionName: "fetch",
        //    functionDescription: "Returns a content response.",
        //    functionParameters: BinaryData.FromObjectAsJson(new
        //    {
        //        type = "object",
        //        properties = new
        //        {
        //            url = new { type = "string", description = "The URL to fetch." }
        //        },
        //        required = new[] { "url" },
        //        additionalProperties = false
        //    }),
        //    strictModeEnabled: false
        //));
        //creation.Tools.Add(ResponseTool.CreateFunctionTool(
        //    functionName: "search",
        //    functionDescription: "Returns a content response.",
        //    functionParameters: BinaryData.FromObjectAsJson(new
        //    {
        //        type = "object",
        //        properties = new
        //        {
        //            q = new { type = "string", description = "The query to search." }
        //        },
        //        required = new[] { "q" },
        //        additionalProperties = false
        //    }),
        //    strictModeEnabled: false
        //));

        AddOptionalTools(options, creation);

        return Task.FromResult((client, creation));
    }

    private static void AddOptionalTools(ResponderOptions options, ResponseCreationOptions creation)
    {
        if (options.EnableWebSearchTool && !creation.Tools.Any(static tool => tool is WebSearchTool))
        {
            creation.Tools.Add(ResponseTool.CreateWebSearchTool());
        }

        if (options.EnableComputerTool && !creation.Tools.Any(static tool => tool is ComputerTool))
        {
            creation.Tools.Add(ResponseTool.CreateComputerTool(ComputerToolEnvironment.Browser, 1920, 1080));
        }
    }
}
