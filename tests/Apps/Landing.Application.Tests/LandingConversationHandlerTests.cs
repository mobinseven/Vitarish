using Xunit;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Landing.Application;

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
}
