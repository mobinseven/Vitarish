using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Landing.Api;
using Vitarish.Landing.Application;
using Xunit;

namespace Vitarish.Landing.Application.Tests.Api;

public sealed class LandingConversationResultMapperTests
{
    [Theory]
    [InlineData(AgentConversationOutcome.HistoryLimitExceeded)]
    [InlineData(AgentConversationOutcome.InvalidHistory)]
    [InlineData(AgentConversationOutcome.ProfileChanged)]
    public async Task Client_triggerable_conversation_states_map_to_409_conflict(AgentConversationOutcome outcome)
    {
        DefaultHttpContext context = Context();

        IResult result = LandingApiEndpointMapping.ToHttpResult(new LandingConversationResult(outcome));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Provider_failure_keeps_dependency_unavailable_semantics()
    {
        DefaultHttpContext context = Context();

        IResult result = LandingApiEndpointMapping.ToHttpResult(new LandingConversationResult(AgentConversationOutcome.ProviderFailure));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task NotFound_stays_404()
    {
        DefaultHttpContext context = Context();

        IResult result = LandingApiEndpointMapping.ToHttpResult(new LandingConversationResult(AgentConversationOutcome.NotFound));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
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
}
