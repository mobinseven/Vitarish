using System.Text.Json;
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

    [Fact]
    public async Task Success_returns_the_reply_and_repeats_its_text_as_the_message()
    {
        DefaultHttpContext context = Context();
        AgentInteractiveReply reply = new("Which kind of clinic?", new AgentInteractiveQuestion("Kind", [new("dental", "Dental"), new("physio", "Physio")], 1), ["online-booking"], LandingNextActions.OpenClinic);

        IResult result = LandingApiEndpointMapping.ToHttpResult(new LandingConversationResult(AgentConversationOutcome.Succeeded, Guid.NewGuid(), reply));
        await result.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Body.Position = 0;
        using JsonDocument body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("message").GetString().Should().Be("Which kind of clinic?");
        body.RootElement.GetProperty("reply").GetProperty("question").GetProperty("options").GetArrayLength().Should().Be(2);
        body.RootElement.GetProperty("reply").GetProperty("nextAction").GetString().Should().Be(LandingNextActions.OpenClinic);
    }

    [Fact]
    public void Features_list_every_catalog_entry_with_its_availability()
    {
        Vitarish.Landing.Contracts.LandingFeaturesResponse features = LandingApiEndpointMapping.GetFeatures();

        features.Features.Select(feature => feature.Key).Should().Equal(LandingFeatureCatalog.Keys);
        features.Features.Single(feature => feature.Key == "tamin-insurance").Availability
            .Should().Be(Vitarish.Landing.Contracts.LandingFeatureAvailability.InProgress);
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
