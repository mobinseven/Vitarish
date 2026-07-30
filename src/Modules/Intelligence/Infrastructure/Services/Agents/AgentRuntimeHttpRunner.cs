using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Agents;

public sealed class AgentRuntimeHttpRunner(
    HttpClient httpClient,
    IOptions<AgentRuntimeOptions> options,
    IAgentRuntimeCredentialService credentialService,
    ILogger<AgentRuntimeHttpRunner> logger) : IAgentRunner
{
    public async Task<AgentRunResult> RunAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ToolCallbackUrl) ||
            string.IsNullOrWhiteSpace(options.Value.BaseUrl) ||
            string.IsNullOrWhiteSpace(options.Value.SharedSecret))
            return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime is not configured.");

        AgentRuntimeRunRequest request = new(
            definition,
            input,
            tools.Select(tool => tool.Definition).ToArray(),
            context,
            limits,
            options.Value.ToolCallbackUrl,
            credentialService.Create(context, TimeSpan.FromMinutes(5)));

        logger.LogInformation(
            "Forwarding patient agent request {ExecutionId} with {ToolCount} tools: {ToolNames}.",
            context.ExecutionId,
            request.Tools.Count,
            string.Join(", ", request.Tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal)));

        try
        {
            Stopwatch latency = Stopwatch.StartNew();
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, "/api/internal/clinic-agent/run")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.SharedSecret);
            using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("The agent runtime returned HTTP {StatusCode}.", (int)response.StatusCode);
                return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime could not complete the request.");
            }

            AgentRuntimeRunResponse? result = await response.Content.ReadFromJsonAsync<AgentRuntimeRunResponse>(cancellationToken).ConfigureAwait(false);
            if (result is null)
                return Failure(AgentRunErrorCode.InvalidProviderResponse, "The agent runtime returned an empty response.");

            return new AgentRunResult(
                result.IsSuccess,
                result.Output,
                result.StructuredOutput,
                result.Usage,
                latency.Elapsed,
                result.Iterations,
                result.ToolCalls,
                result.Error)
            {
                ResponseId = result.ResponseId,
                ToolExecutions = result.ToolExecutions ?? []
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(AgentRunErrorCode.Cancelled, "The agent execution was cancelled.");
        }
        catch (JsonException)
        {
            return Failure(AgentRunErrorCode.InvalidProviderResponse, "The agent runtime returned an invalid response.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogError("The agent runtime request failed. Failure type: {FailureType}.", exception.GetType().Name);
            return Failure(AgentRunErrorCode.ProviderUnavailable, "The agent runtime could not be reached.");
        }
    }

    private static AgentRunResult Failure(AgentRunErrorCode code, string message) => new(
        false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(code, message));
}
