using OllamaSharp;
using OllamaSharp.ModelContextProtocol;
using OllamaSharp.ModelContextProtocol.Server;
using OllamaSharp.Models.Chat;
using OllamaSharp.Tools;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Vorn.Ai.Server.Domain.Models;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Interfaces;

namespace Vorn.Ai.Server.Infrastructure.Services.Ollama;

public sealed class OllamaResponder(VornOptions vornOptions) : IResponder
{
    private OllamaApiClient? _client;
    private readonly DefaultToolInvoker _toolInvoker = new();
    private readonly ConcurrentDictionary<string, PendingToolCall> _pendingToolCalls = new();

    private OllamaApiClient Client => _client ??= new OllamaApiClient(vornOptions.Ai.Ollama.BaseUrl);

    public async Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        List<ResponderOutput> outputs = [];

        await foreach(ResponderOutput item in StreamInternalAsync(input, options, ct))
        {
            outputs.Add(item);
        }

        return outputs;
    }

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach(ResponderOutput item in StreamInternalAsync(input, options, ct))
        {
            yield return item;
        }
    }

    private async IAsyncEnumerable<ResponderOutput> StreamInternalAsync(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string model = string.IsNullOrWhiteSpace(options.Model)
            ? vornOptions.Ai.Ollama.DefaultModel
            : options.Model!.Trim();

        List<ResponderInput> approvals = [];
        List<ResponderInput> otherInputs = [];

        foreach(ResponderInput item in input)
        {
            if(item.Type == ResponderInputType.McpToolCallApproval && !string.IsNullOrWhiteSpace(item.ApprovalId))
            {
                approvals.Add(item);
                continue;
            }

            otherInputs.Add(item);
        }

        if(approvals.Count > 0)
        {
            foreach(ResponderInput approval in approvals)
            {
                await foreach(ResponderOutput output in ProcessPendingToolApprovalAsync(approval.ApprovalId!, ct))
                {
                    yield return output;
                }
            }

            if(otherInputs.Count == 0)
            {
                yield break;
            }
        }

        List<Message> messages = BuildInitialMessages(otherInputs, options);
        McpToolsContext? toolsContext = await BuildMcpToolsContextAsync(options, ct);

        if(toolsContext?.RequiresApproval == true)
        {
            int insertIndex = 0;

            while(insertIndex < messages.Count && messages[insertIndex].Role == ChatRole.System)
            {
                insertIndex++;
            }
        }

        List<ResponderOutput> aggregatedOutputs = [];

        await foreach(ResponderOutput output in RunConversationLoopAsync(messages, model, toolsContext, aggregatedOutputs, ct))
        {
            yield return output;
        }
    }

    private async IAsyncEnumerable<ResponderOutput> ProcessPendingToolApprovalAsync(
        string approvalId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if(!_pendingToolCalls.TryRemove(approvalId, out PendingToolCall? pending))
        {
            yield break;
        }

        List<Message> messageHistory = CloneMessages(pending.History);

        List<ResponderOutput> aggregatedOutputs = [];

        ToolResult toolResult = await _toolInvoker.InvokeAsync(pending.ToolCall, pending.ToolsContext.Tools, ct);
        string toolResultText = toolResult.Result?.ToString() ?? string.Empty;

        Message toolMessage = new(ChatRole.Tool, toolResultText)
        {
            ToolName = pending.ToolCall.Function?.Name
        };

        messageHistory.Add(toolMessage);

        await foreach(ResponderOutput output in RunConversationLoopAsync(messageHistory, pending.Model, pending.ToolsContext, aggregatedOutputs, ct))
        {
            yield return output;
        }
    }

    private async IAsyncEnumerable<ResponderOutput> RunConversationLoopAsync(
        List<Message> messageHistory,
        string model,
        McpToolsContext? toolsContext,
        List<ResponderOutput> aggregatedOutputs,
        [EnumeratorCancellation] CancellationToken ct)
    {
        bool continueLoop = true;

        while(continueLoop)
        {
            ChatRequest request = new()
            {
                Model = model,
                Messages = messageHistory,
                Stream = true,
                Tools = toolsContext?.Tools
            };

            MessageBuilder builder = new();

            await foreach(ChatResponseStream? stream in Client.ChatAsync(request, ct))
            {
                if(stream?.Message?.Thinking is { Length: > 0 } thinking)
                {
                    ResponderOutput reasoningOutput = ResponderOutput.ReasoningOutput(thinking);
                    aggregatedOutputs.Add(reasoningOutput);
                    yield return reasoningOutput;
                }

                if(stream?.Message?.Content is { Length: > 0 } content)
                {
                    ResponderOutput textOutput = ResponderOutput.TextOutput(content);
                    aggregatedOutputs.Add(textOutput);
                    yield return textOutput;
                }

                builder.Append(stream);
            }

            if(!builder.HasValue)
            {
                break;
            }

            Message assistantMessage = builder.ToMessage();
            messageHistory.Add(assistantMessage);

            bool executedTool = false;
            bool approvalRequired = false;

            if(assistantMessage.ToolCalls is { } toolCalls && toolsContext is not null)
            {
                List<string>? pendingApprovals = null;

                foreach(Message.ToolCall toolCall in toolCalls)
                {
                    string toolName = toolCall.Function?.Name ?? "unknown";
                    string callId = string.IsNullOrWhiteSpace(toolCall.Id)
                        ? Guid.NewGuid().ToString("N")
                        : toolCall.Id!;

                    toolCall.Id ??= callId;

                    string? serializedArguments = SerializeArguments(toolCall.Function?.Arguments);

                    McpToolContext? context = toolsContext.TryGetContext(toolName, out McpToolContext? ctx)
                        ? ctx
                        : null;

                    ResponderOutput callOutput = ResponderOutput.McpToolCallOutput(toolName, serializedArguments, context?.ServerLabel, callId);
                    aggregatedOutputs.Add(callOutput);
                    yield return callOutput;

                    if(context is null)
                    {
                        continue;
                    }

                    if(context.RequireApproval)
                    {
                        PendingToolCall pending = new(
                            CloneMessages(messageHistory),
                            toolsContext,
                            CloneToolCall(toolCall, callId),
                            model);

                        _pendingToolCalls[callId] = pending;
                        (pendingApprovals ??= []).Add(callId);

                        ResponderOutput approvalOutput = ResponderOutput.McpApprovalRequestOutput(toolName, serializedArguments, context.ServerLabel, callId);
                        aggregatedOutputs.Add(approvalOutput);
                        yield return approvalOutput;

                        approvalRequired = true;
                    }
                    else
                    {
                        ToolResult toolResult = await _toolInvoker.InvokeAsync(toolCall, toolsContext.Tools, ct);
                        string toolResultText = toolResult.Result?.ToString() ?? string.Empty;

                        Message toolMessage = new(ChatRole.Tool, toolResultText)
                        {
                            ToolName = toolCall.Function?.Name
                        };

                        messageHistory.Add(toolMessage);
                        executedTool = true;
                    }
                }

                if(pendingApprovals is { Count: > 0 })
                {
                    List<Message>? latestHistory = null;

                    foreach(string pendingCallId in pendingApprovals)
                    {
                        if(_pendingToolCalls.TryGetValue(pendingCallId, out PendingToolCall? pendingCall))
                        {
                            latestHistory ??= CloneMessages(messageHistory);
                            _pendingToolCalls[pendingCallId] = pendingCall with { History = latestHistory };
                        }
                    }
                }

                if(approvalRequired)
                {
                    break;
                }

                if(executedTool)
                {
                    continue;
                }
            }

            continueLoop = false;
        }

        if(aggregatedOutputs.Count > 0)
        {
            yield return ResponderOutput.CompletionOutput(
                responseId: null,
                content: [.. aggregatedOutputs],
                metadata: null);
        }
    }

    private static List<Message> BuildInitialMessages(IReadOnlyList<ResponderInput> input, ResponderOptions options)
    {
        List<Message> messages = [];

        if(!string.IsNullOrWhiteSpace(options.Instructions))
        {
            messages.Add(new Message(ChatRole.System, options.Instructions));
        }

        foreach(ResponderInput item in input)
        {
            switch(item.Type)
            {
                case ResponderInputType.Text:
                    messages.Add(new Message(ChatRole.User, item.Text ?? string.Empty));
                    break;
                case ResponderInputType.Image:
                    messages.Add(new Message(ChatRole.User, item.ImageUrl ?? string.Empty, [item.ImageUrl ?? string.Empty]));
                    break;
                case ResponderInputType.File:
                    messages.Add(new Message(ChatRole.User, $"File: {item.Filename ?? "unknown"}"));
                    break;
            }
        }

        return messages;
    }

    private async Task<McpToolsContext?> BuildMcpToolsContextAsync(ResponderOptions options, CancellationToken ct)
    {
        if(options.Tools is null || options.Tools.Count == 0)
        {
            return null;
        }

        List<object> toolInstances = [];
        Dictionary<string, McpToolContext> contexts = new(StringComparer.OrdinalIgnoreCase);
        bool requiresApproval = false;

        foreach(ResponderToolDefinition tool in options.Tools)
        {
            if(!string.Equals(tool.Type, "mcp", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if(string.IsNullOrWhiteSpace(tool.ServerUrl))
            {
                continue;
            }

            string serverLabel = string.IsNullOrWhiteSpace(tool.ServerLabel)
                ? tool.ServerUrl
                : tool.ServerLabel;

            McpServerConfiguration configuration = new()
            {
                Name = serverLabel,
                Command = tool.ServerUrl!,
                TransportType = McpServerTransportType.Sse
            };

            if(!string.IsNullOrWhiteSpace(tool.ServerToken))
            {
                configuration.Environment = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {tool.ServerToken}"
                };
            }

            McpClientTool[] availableTools;

            try
            {
                availableTools = await Tools.GetFromMcpServers(configuration);
            }
            catch
            {
                continue;
            }

            HashSet<string>? allowed = tool.AllowedTools is { Count: > 0 }
                ? new HashSet<string>(tool.AllowedTools, StringComparer.OrdinalIgnoreCase)
                : null;

            foreach(McpClientTool mcpTool in availableTools)
            {
                string? toolName = mcpTool.Function?.Name;

                if(string.IsNullOrWhiteSpace(toolName))
                {
                    continue;
                }

                if(allowed is not null && !allowed.Contains(toolName))
                {
                    continue;
                }

                toolInstances.Add(mcpTool);
                bool approvalRequired = tool.RequireApproval == ResponderToolApprovalRequirement.Always;
                contexts[toolName] = new McpToolContext(mcpTool, serverLabel, approvalRequired);
                requiresApproval |= approvalRequired;
            }
        }

        return toolInstances.Count == 0
            ? null
            : new McpToolsContext(toolInstances, contexts, requiresApproval);
    }

    private static List<Message> CloneMessages(IEnumerable<Message> source)
    {
        List<Message> clone = [];

        foreach(Message message in source)
        {
            Message copy = new()
            {
                Role = message.Role,
                Content = message.Content,
                Thinking = message.Thinking,
                ToolName = message.ToolName
            };

            if(message.Images is { Length: > 0 } images)
            {
                copy.Images = images.ToArray();
            }

            if(message.ToolCalls is { } toolCalls)
            {
                List<Message.ToolCall> clonedCalls = [];

                foreach(Message.ToolCall call in toolCalls)
                {
                    clonedCalls.Add(CloneToolCall(call, call.Id ?? Guid.NewGuid().ToString("N")));
                }

                copy.ToolCalls = clonedCalls;
            }

            clone.Add(copy);
        }

        return clone;
    }

    private static Message.ToolCall CloneToolCall(Message.ToolCall source, string callId)
    {
        Dictionary<string, object?>? arguments = null;

        if(source.Function?.Arguments is { } args)
        {
            arguments = new Dictionary<string, object?>(args);
        }

        return new Message.ToolCall
        {
            Id = callId,
            Function = source.Function is null
                ? null
                : new Message.Function
                {
                    Index = source.Function.Index,
                    Name = source.Function.Name,
                    Arguments = arguments
                }
        };
    }

    private static string? SerializeArguments(IDictionary<string, object?>? arguments)
    {
        return arguments is null ? null : JsonSerializer.Serialize(arguments);
    }

    private sealed record McpToolContext(McpClientTool Tool, string ServerLabel, bool RequireApproval);

    private sealed record McpToolsContext(
        IReadOnlyList<object> Tools,
        Dictionary<string, McpToolContext> ContextByName,
        bool RequiresApproval)
    {
        public bool TryGetContext(string toolName, out McpToolContext? context)
        {
            return ContextByName.TryGetValue(toolName, out context);
        }
    }

    private sealed record PendingToolCall(
        List<Message> History,
        McpToolsContext ToolsContext,
        Message.ToolCall ToolCall,
        string Model);
}
