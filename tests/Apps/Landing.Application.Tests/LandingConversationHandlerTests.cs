using System.Text.Json;
using Xunit;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Landing.Application;
using FluentAssertions;

namespace Vitarish.Landing.Application.Tests;

public sealed class LandingConversationHandlerTests
{
    [Fact]
    public async Task Run_UsesResolvedTenantAndPublicSessionInScope()
    {
        RunAgentConversation? forwarded = null;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<RunAgentConversation>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => forwarded = (RunAgentConversation)request)
            .ReturnsAsync(new RunAgentConversationResult(AgentConversationOutcome.ProviderFailure));
        RunLandingConversationHandler handler = new(
            sender.Object,
            Options.Create(new LandingConversationAgentOptions { Model = "configured-model" }));

        _ = await handler.Handle(new RunLandingConversation(
            "resolved-tenant", "public-session", "about", "hello", null), default);

        Assert.NotNull(forwarded);
        Assert.Equal("resolved-tenant", forwarded.Scope.TenantId);
        Assert.Equal("public-session", forwarded.Scope.CreatingUserId);
        Assert.Equal("about", forwarded.Scope.SubjectId);
        Assert.Equal("configured-model", forwarded.Definition.Model);
    }

    [Fact]
    public async Task Delete_UsesResolvedTenantAndPublicSessionInScope()
    {
        DeleteAgentConversation? forwarded = null;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<DeleteAgentConversation>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => forwarded = (DeleteAgentConversation)request)
            .ReturnsAsync(AgentConversationOutcome.Succeeded);
        DeleteLandingConversationHandler handler = new(sender.Object);
        Guid conversationId = Guid.NewGuid();

        _ = await handler.Handle(new DeleteLandingConversation(
            "resolved-tenant", "public-session", "contact", conversationId), default);

        Assert.NotNull(forwarded);
        Assert.Equal(conversationId, forwarded.ConversationId);
        Assert.Equal("resolved-tenant", forwarded.Scope.TenantId);
        Assert.Equal("public-session", forwarded.Scope.CreatingUserId);
        Assert.Equal("contact", forwarded.Scope.SubjectId);
    }

    [Fact]
    public async Task Run_asks_for_an_interactive_reply_limited_to_the_feature_catalog()
    {
        RunAgentConversation? forwarded = null;
        RunLandingConversationHandler handler = Handler(request => forwarded = request, new RunAgentConversationResult(AgentConversationOutcome.ProviderFailure));

        _ = await handler.Handle(new RunLandingConversation("tenant", "session", "home", "hello", null), default);

        using JsonDocument schema = JsonDocument.Parse(forwarded!.Definition.Result!.JsonSchema);
        schema.RootElement.GetProperty("properties").GetProperty("references").GetProperty("items").GetProperty("enum")
            .EnumerateArray().Select(item => item.GetString()).Should().Equal(LandingFeatureCatalog.Keys);
        forwarded.Definition.Instructions.Should().Contain(LandingNextActions.OpenClinic);
    }

    [Fact]
    public async Task Run_refuses_to_save_a_turn_whose_reply_has_no_text()
    {
        RunAgentConversation? forwarded = null;
        RunLandingConversationHandler handler = Handler(request => forwarded = request, new RunAgentConversationResult(AgentConversationOutcome.ProviderFailure));
        _ = await handler.Handle(new RunLandingConversation("tenant", "session", "home", "hello", null), default);

        AgentConversationOutcome outcome = await forwarded!.BeforeCommit!(Guid.NewGuid(), Run("{}"), default);

        outcome.Should().Be(AgentConversationOutcome.ProviderFailure);
    }

    [Fact]
    public async Task Run_returns_the_validated_reply()
    {
        Guid conversationId = Guid.NewGuid();
        AgentRunResult run = Run("""{"text":"Hi","question":null,"references":["reminders","made-up"],"nextAction":"open-clinic"}""");
        RunLandingConversationHandler handler = Handler(_ => { }, new RunAgentConversationResult(AgentConversationOutcome.Succeeded, conversationId, run));

        LandingConversationResult result = await handler.Handle(new RunLandingConversation("tenant", "session", "home", "hello", null), default);

        result.ConversationId.Should().Be(conversationId);
        result.Reply!.References.Should().Equal("reminders");
        result.Reply.NextAction.Should().Be(LandingNextActions.OpenClinic);
    }

    private static RunLandingConversationHandler Handler(Action<RunAgentConversation> capture, RunAgentConversationResult result)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<RunAgentConversation>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => capture((RunAgentConversation)request))
            .ReturnsAsync(result);
        return new(sender.Object, Options.Create(new LandingConversationAgentOptions { Model = "model" }));
    }

    private static AgentRunResult Run(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return new AgentRunResult(true, json, document.RootElement.Clone(), new(1, 1), TimeSpan.Zero, 1, 0);
    }
}
