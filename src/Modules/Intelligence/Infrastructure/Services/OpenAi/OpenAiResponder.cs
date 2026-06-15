using Microsoft.Extensions.Options;
using OpenAI.Responses;
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

    public async Task<IReadOnlyList<ResponderOutput>> GenerateAsync(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options,
        CancellationToken ct = default)
    {
        IReadOnlyList<ResponseItem> items = [.. ResponderHelpers.MapInput(input)];

        (ResponsesClient client, CreateResponseOptions creation) =
            CreateResponseClient(options, items, streaming: false);

        ResponseResult response = await client.CreateResponseAsync(creation, ct);

        IReadOnlyList<ResponderOutput> mapped = ResponderHelpers.MapResponse(response);

        return mapped;
    }

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyList<ResponseItem> items = [.. ResponderHelpers.MapInput(input)];

        (ResponsesClient client, CreateResponseOptions creation) =
            CreateResponseClient(options, items, streaming: true);

        System.ClientModel.AsyncCollectionResult<StreamingResponseUpdate> updates =
            client.CreateResponseStreamingAsync(creation, ct);

        await foreach (StreamingResponseUpdate update in updates.WithCancellation(ct))
        {
            switch (update)
            {
                case StreamingResponseOutputTextDeltaUpdate textDelta:
                    yield return ResponderOutput.TextOutput(textDelta.Delta);
                    break;

                case StreamingResponseCompletedUpdate completed:
                    foreach (ResponderOutput item in ResponderHelpers
                        .MapResponse(completed.Response)
                        .Where(static r => r.Type != ResponderOutputType.Text))
                    {
                        yield return item;
                    }

                    break;
            }
        }
    }

    private (ResponsesClient client, CreateResponseOptions creation) CreateResponseClient(
        ResponderOptions options,
        IReadOnlyList<ResponseItem> inputItems,
        bool streaming)
    {
        AiOptions optionsValue = aiOptions.CurrentValue;

        string model = string.IsNullOrWhiteSpace(options.Model)
            ? optionsValue.DefaultResponseModel
            : options.Model!.Trim();

        ResponsesClient client = new(apiKey: optionsValue.OpenAi.ApiKey);

        CreateResponseOptions creation = new(model, inputItems);

        if (streaming)
        {
            creation.StreamingEnabled = true;
        }

        if (!string.IsNullOrWhiteSpace(options.Instructions))
        {
            creation.Instructions = options.Instructions;
        }

        if (!string.IsNullOrWhiteSpace(options.PreviousResponseId))
        {
            creation.PreviousResponseId = options.PreviousResponseId;
        }

        creation.TextOptions = new ResponseTextOptions
        {
            TextFormat = options.TextFormat switch
            {
                DomainResponseTextFormat.JsonSchema when options.JsonSchema is not null =>
                    OpenAiResponseTextFormat.CreateJsonSchemaFormat(
                        options.JsonSchema.Name,
                        BinaryData.FromString(options.JsonSchema.Schema),
                        options.JsonSchema.Description,
                        options.JsonSchema.Strict),

                DomainResponseTextFormat.JsonObject =>
                    OpenAiResponseTextFormat.CreateJsonObjectFormat(),

                _ =>
                    OpenAiResponseTextFormat.CreateTextFormat()
            }
        };

        AddMcpTools(options, creation);
        AddOptionalTools(options, creation);

        return (client, creation);
    }

    private static void AddMcpTools(ResponderOptions options, CreateResponseOptions creation)
    {
        foreach (ResponderToolDefinition tool in options.Tools ?? Array.Empty<ResponderToolDefinition>())
        {
            if (!string.Equals(tool.Type, "mcp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(tool.ServerLabel) ||
                string.IsNullOrWhiteSpace(tool.ServerUrl))
            {
                continue;
            }

            if (!Uri.TryCreate(tool.ServerUrl, UriKind.Absolute, out Uri? serverUri))
            {
                continue;
            }

            GlobalMcpToolCallApprovalPolicy globalApprovalPolicy = tool.RequireApproval switch
            {
                ResponderToolApprovalRequirement.Always =>
                    GlobalMcpToolCallApprovalPolicy.AlwaysRequireApproval,

                _ =>
                    GlobalMcpToolCallApprovalPolicy.NeverRequireApproval
            };

            McpTool mcpTool = new(
                serverLabel: tool.ServerLabel.Trim(),
                serverUri: serverUri)
            {
                ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(globalApprovalPolicy)
            };

            if (!string.IsNullOrWhiteSpace(tool.ServerToken))
            {
                mcpTool.AuthorizationToken = NormalizeBearerToken(tool.ServerToken);
            }

            McpToolFilter? allowedTools = CreateAllowedToolsFilter(tool.AllowedTools);

            if (allowedTools is not null)
            {
                mcpTool.AllowedTools = allowedTools;
            }

            creation.Tools.Add(mcpTool);
        }
    }

    private static McpToolFilter? CreateAllowedToolsFilter(IReadOnlyCollection<string>? allowedTools)
    {
        if (allowedTools is not { Count: > 0 })
        {
            return null;
        }

        McpToolFilter filter = new();

        foreach (string allowedTool in allowedTools)
        {
            if (!string.IsNullOrWhiteSpace(allowedTool))
            {
                filter.ToolNames.Add(allowedTool.Trim());
            }
        }

        return filter.ToolNames.Count > 0 ? filter : null;
    }

    private static string NormalizeBearerToken(string token)
    {
        string trimmed = token.Trim();

        return trimmed.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? trimmed["Bearer ".Length..].Trim()
            : trimmed;
    }

    private static void AddOptionalTools(ResponderOptions options, CreateResponseOptions creation)
    {
        if (options.EnableWebSearchTool &&
            !creation.Tools.Any(static tool => tool is WebSearchTool))
        {
            creation.Tools.Add(ResponseTool.CreateWebSearchTool());
        }

        if (options.EnableComputerTool &&
            !creation.Tools.Any(static tool => tool is ComputerTool))
        {
            creation.Tools.Add(ResponseTool.CreateComputerTool(
                ComputerToolEnvironment.Browser,
                1920,
                1080));
        }
    }
}
