using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Tests.Api;

public sealed class IntelligenceAgentConversationResultMapperTests
{
    [Fact]
    public async Task History_limit_exceeded_maps_to_409_conflict_instead_of_502()
    {
        DefaultHttpContext context = Context();

        IResult result = IntelligenceAgentConversationResultMapper.ToHttpResult(
            new AgentConversationEndpointResult(AgentConversationOutcome.HistoryLimitExceeded));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        IntelligenceError? error = JsonSerializer.Deserialize<IntelligenceError>(
            await ReadBodyAsync(context),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        error!.Code.Should().Be(IntelligenceErrorCode.InvalidHistory);
    }

    [Fact]
    public async Task Invalid_history_still_maps_to_409_conflict()
    {
        DefaultHttpContext context = Context();

        IResult result = IntelligenceAgentConversationResultMapper.ToHttpResult(
            new AgentConversationEndpointResult(AgentConversationOutcome.InvalidHistory));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Provider_failure_keeps_dependency_failure_semantics()
    {
        DefaultHttpContext context = Context();

        IResult result = IntelligenceAgentConversationResultMapper.ToHttpResult(
            new AgentConversationEndpointResult(AgentConversationOutcome.ProviderFailure));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    private static DefaultHttpContext Context()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddOptions();
        DefaultHttpContext context = new() { RequestServices = services.BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        _ = context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
