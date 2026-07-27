using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

public sealed class OpenAiAgentRunner : IAgentRunner
{
    private readonly IAgentToolDispatcher dispatcher;
    private readonly IOpenAiAgentClient client;

    public OpenAiAgentRunner(
        IAgentToolDispatcher dispatcher,
        IOptionsMonitor<AiOptions> options,
        OpenAiAgentRequestMapper requestMapper,
        OpenAiAgentResponseMapper responseMapper)
        : this(dispatcher, new OpenAiResponsesAgentClient(
            options.CurrentValue.OpenAi.ApiKey,
            requestMapper,
            responseMapper))
    {
    }

    internal OpenAiAgentRunner(IAgentToolDispatcher dispatcher, IOpenAiAgentClient client)
    {
        this.dispatcher = dispatcher;
        this.client = client;
    }

    public async Task<AgentRunResult> RunAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        CancellationToken cancellationToken = default)
    {
        AgentRunError? validationError = Validate(definition, input, tools, context, limits);
        if (validationError is not null)
        {
            return Failure(validationError, 0, 0, 0, 0, TimeSpan.Zero);
        }

        Dictionary<string, IAgentTool> registry = tools.ToDictionary(
            static tool => tool.Definition.Name,
            StringComparer.Ordinal);
        List<AgentToolDefinition> definitions = [.. registry.Values.Select(static tool => tool.Definition)];
        string? userInput = input.UserInput;
        string? previousResponseId = null;
        List<AgentToolResult> toolResults = [];
        int inputTokens = 0;
        int outputTokens = 0;
        int toolCallCount = 0;
        TimeSpan providerLatency = TimeSpan.Zero;

        using CancellationTokenSource timeoutSource = new(limits.EffectiveTimeout);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        for (int iteration = 1; iteration <= limits.MaxIterations; iteration++)
        {
            OpenAiAgentResponse response;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                response = await client.CreateResponseAsync(
                    new OpenAiAgentRequest(
                        definition,
                        userInput,
                        definitions,
                        toolResults,
                        previousResponseId,
                        Math.Max(1, limits.MaxTokens - outputTokens)),
                    linkedSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return Failure(new(AgentRunErrorCode.Timeout, "The agent execution timed out."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency + stopwatch.Elapsed);
            }
            catch (OperationCanceledException)
            {
                return Failure(new(AgentRunErrorCode.Cancelled, "The agent execution was cancelled."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency + stopwatch.Elapsed);
            }
            catch (Exception)
            {
                return Failure(new(AgentRunErrorCode.ProviderFailure, "The AI provider could not complete the request."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency + stopwatch.Elapsed);
            }
            finally
            {
                stopwatch.Stop();
            }

            providerLatency += stopwatch.Elapsed;
            inputTokens += response.InputTokens;
            outputTokens += response.OutputTokens;

            if (inputTokens + outputTokens > limits.MaxTokens)
            {
                return Failure(new(AgentRunErrorCode.TokenLimitExceeded, "The agent token limit was exceeded."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
            }

            if (response.ToolCalls.Count == 0)
            {
                if (response.Output is null)
                {
                    return Failure(new(AgentRunErrorCode.InvalidProviderResponse, "The provider returned neither a result nor a tool call."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }

                JsonElement? structuredOutput = null;
                if (definition.Result is not null)
                {
                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(response.Output);
                        structuredOutput = document.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        return Failure(new(AgentRunErrorCode.InvalidStructuredResult, "The provider returned an invalid structured result."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                    }
                }

                return new AgentRunResult(true, response.Output, structuredOutput, new(inputTokens, outputTokens), providerLatency, iteration, toolCallCount);
            }

            if (toolCallCount + response.ToolCalls.Count > limits.MaxToolCalls)
            {
                return Failure(new(AgentRunErrorCode.ToolCallLimitExceeded, "The agent tool-call limit was exceeded."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
            }

            toolResults = [];
            foreach (AgentToolCall call in response.ToolCalls)
            {
                if (!registry.ContainsKey(call.Name))
                {
                    return Failure(new(AgentRunErrorCode.UnknownTool, "The provider requested a tool that was not registered."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }

                try
                {
                    using JsonDocument _ = JsonDocument.Parse(call.ArgumentsJson);
                }
                catch (JsonException)
                {
                    return Failure(new(AgentRunErrorCode.MalformedToolArguments, "The provider supplied malformed tool arguments."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }

                AgentToolResult result;
                try
                {
                    result = await dispatcher.DispatchAsync(call, registry, context, linkedSource.Token);
                }
                catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    return Failure(new(AgentRunErrorCode.Timeout, "The agent execution timed out."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }
                catch (OperationCanceledException)
                {
                    return Failure(new(AgentRunErrorCode.Cancelled, "The agent execution was cancelled."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }
                catch (Exception)
                {
                    return Failure(new(AgentRunErrorCode.ToolFailure, "A registered tool failed."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }

                if (!string.Equals(result.CallId, call.Id, StringComparison.Ordinal))
                {
                    return Failure(new(AgentRunErrorCode.ToolFailure, "A registered tool returned a result for a different call."), inputTokens, outputTokens, iteration, toolCallCount, providerLatency);
                }

                toolResults.Add(result);
                toolCallCount++;
            }

            previousResponseId = response.Id;
            userInput = null;
        }

        return Failure(new(AgentRunErrorCode.IterationLimitExceeded, "The agent iteration limit was exceeded."), inputTokens, outputTokens, limits.MaxIterations, toolCallCount, providerLatency);
    }

    private static AgentRunError? Validate(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits)
    {
        if (string.IsNullOrWhiteSpace(definition.Model) ||
            string.IsNullOrWhiteSpace(definition.Instructions) ||
            string.IsNullOrWhiteSpace(input.UserInput) ||
            string.IsNullOrWhiteSpace(context.ExecutionId) ||
            limits.MaxIterations <= 0 || limits.MaxToolCalls < 0 || limits.MaxTokens <= 0 ||
            limits.EffectiveTimeout <= TimeSpan.Zero)
        {
            return new(AgentRunErrorCode.InvalidRequest, "The agent request or execution limits are invalid.");
        }

        if (tools.Any(static tool =>
                string.IsNullOrWhiteSpace(tool.Definition.Name) ||
                string.IsNullOrWhiteSpace(tool.Definition.Description) ||
                string.IsNullOrWhiteSpace(tool.Definition.ParametersJsonSchema)) ||
            tools.GroupBy(static tool => tool.Definition.Name, StringComparer.Ordinal).Any(static group => group.Count() > 1))
        {
            return new(AgentRunErrorCode.InvalidRequest, "Agent tools must have unique, complete definitions.");
        }

        return null;
    }

    private static AgentRunResult Failure(
        AgentRunError error,
        int inputTokens,
        int outputTokens,
        int iterations,
        int toolCalls,
        TimeSpan latency) => new(
            false,
            null,
            null,
            new(inputTokens, outputTokens),
            latency,
            iterations,
            toolCalls,
            error);
}
