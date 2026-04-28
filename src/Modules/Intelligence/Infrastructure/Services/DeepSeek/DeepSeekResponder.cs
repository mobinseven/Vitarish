using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.DeepSeek;

/// <summary>
/// Implements <see cref="IResponder"/> using the DeepSeek Chat Completions API,
/// which is compatible with the OpenAI chat completions protocol.
/// MCP tool-calling is not supported by DeepSeek and is intentionally omitted.
/// </summary>
public sealed class DeepSeekResponder(IOptionsMonitor<AiOptions> aiOptions) : IResponder
{
    // deepseek-reasoner wraps chain-of-thought content in <think>…</think> tags.
    private static readonly Regex ThinkBlockPattern = new(@"<think>([\s\S]*?)</think>", RegexOptions.Compiled);

    public async Task<IReadOnlyList<ResponderOutput>> GenerateAsync(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options,
        CancellationToken ct = default)
    {
        List<ResponderOutput> outputs = [];

        await foreach(ResponderOutput item in StreamAsync(input, options, ct).ConfigureAwait(false))
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
        ChatClient client = CreateClient(options);
        IReadOnlyList<ChatMessage> messages = BuildMessages(input, options);

        List<ResponderOutput> aggregatedOutputs = [];
        string accumulatedText = string.Empty;

        AsyncCollectionResult<StreamingChatCompletionUpdate> stream = client.CompleteChatStreamingAsync(messages, cancellationToken: ct);

        await foreach(StreamingChatCompletionUpdate update in stream.WithCancellation(ct).ConfigureAwait(false))
        {
            foreach(ChatMessageContentPart part in update.ContentUpdate)
            {
                if(string.IsNullOrEmpty(part.Text))
                {
                    continue;
                }

                accumulatedText += part.Text;
            }
        }

        // Extract and surface <think>…</think> reasoning blocks produced by deepseek-reasoner
        // before emitting the visible text response.
        string visibleText = ExtractReasoningBlocks(accumulatedText, out IReadOnlyList<string> reasoningBlocks);

        foreach(string reasoning in reasoningBlocks)
        {
            ResponderOutput reasoningOutput = ResponderOutput.ReasoningOutput(reasoning);
            aggregatedOutputs.Add(reasoningOutput);
            yield return reasoningOutput;
        }

        if(!string.IsNullOrEmpty(visibleText))
        {
            ResponderOutput textOutput = ResponderOutput.TextOutput(visibleText);
            aggregatedOutputs.Add(textOutput);
            yield return textOutput;
        }

        if(aggregatedOutputs.Count > 0)
        {
            yield return ResponderOutput.CompletionOutput(responseId: null, content: [.. aggregatedOutputs], metadata: null);
        }
    }

    private ChatClient CreateClient(ResponderOptions options)
    {
        DeepSeekOptions deepSeekOptions = aiOptions.CurrentValue.DeepSeek;

        string model = string.IsNullOrWhiteSpace(options.Model)
            ? aiOptions.CurrentValue.DefaultResponseModel
            : options.Model!.Trim();

        ApiKeyCredential credential = new(deepSeekOptions.ApiKey);
        OpenAIClientOptions clientOptions = new() { Endpoint = new Uri(deepSeekOptions.BaseUrl) };

        return new ChatClient(model, credential, clientOptions);
    }

    private static IReadOnlyList<ChatMessage> BuildMessages(
        IReadOnlyList<ResponderInput> input,
        ResponderOptions options)
    {
        List<ChatMessage> messages = [];

        if(!string.IsNullOrWhiteSpace(options.Instructions))
        {
            messages.Add(new SystemChatMessage(options.Instructions));
        }

        foreach(ResponderInput item in input)
        {
            switch(item.Type)
            {
                case ResponderInputType.Text:
                    messages.Add(new UserChatMessage(item.Text ?? string.Empty));
                    break;
                case ResponderInputType.Image when item.ImageUrl is not null:
                    messages.Add(new UserChatMessage(ChatMessageContentPart.CreateImagePart(new Uri(item.ImageUrl))));
                    break;
                case ResponderInputType.File:
                    messages.Add(new UserChatMessage($"File: {item.Filename ?? "unknown"}"));
                    break;
            }
        }

        return messages;
    }

    /// <summary>
    /// Strips <c>&lt;think&gt;…&lt;/think&gt;</c> blocks from <paramref name="raw"/>, returns the
    /// trimmed visible text, and populates <paramref name="reasoningBlocks"/> with each extracted block.
    /// </summary>
    private static string ExtractReasoningBlocks(string raw, out IReadOnlyList<string> reasoningBlocks)
    {
        List<string> blocks = [];

        string visible = ThinkBlockPattern.Replace(raw, match =>
        {
            string content = match.Groups[1].Value.Trim();

            if(!string.IsNullOrEmpty(content))
            {
                blocks.Add(content);
            }

            return string.Empty;
        });

        reasoningBlocks = blocks;
        return visible.Trim();
    }
}
