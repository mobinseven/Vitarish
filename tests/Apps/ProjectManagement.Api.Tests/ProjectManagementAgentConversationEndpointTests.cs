using FluentAssertions;
using MediatR;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.ProjectManagement.Api;
using Vitarish.ProjectManagement.Application;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Xunit;

namespace Vitarish.ProjectManagement.Api.Tests;

public sealed class ProjectManagementAgentConversationEndpointTests
{
    [Fact]
    public void Registers_only_the_event_conversation_route()
    {
        ProjectManagementAgentConversationEndpoint endpoint = new(
            RequestContext(Guid.NewGuid(), Guid.NewGuid().ToString("D")).Object,
            Member().Object,
            Access().Object,
            new Mock<ISender>().Object);

        endpoint.Routes.Should().ContainSingle(route =>
            route.AgentKey == "project-management.event" && route.SubjectKind == "event");
    }

    [Fact]
    public async Task Replaces_the_public_current_marker_with_the_authenticated_tenant_and_user()
    {
        Guid tenantId = Guid.NewGuid();
        string userId = Guid.NewGuid().ToString("D");
        Mock<IRequestContext> requestContext = RequestContext(tenantId, userId);
        Mock<IUsersService> users = Member();
        Mock<ISender> sender = new();
        RunProjectManagementAgent? received = null;
        sender.Setup(value => value.Send(It.IsAny<RunProjectManagementAgent>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<global::Vitarish.ProjectManagement.Application.ProjectManagementAgentResult>, CancellationToken>((request, _) => received = (RunProjectManagementAgent)request)
            .ReturnsAsync(new ProjectManagementAgentResult(AgentConversationOutcome.Succeeded, "run", Guid.NewGuid(), System.Text.Json.JsonSerializer.SerializeToElement(new { })));
        ProjectManagementAgentConversationEndpoint endpoint = new(requestContext.Object, users.Object, Access().Object, sender.Object);

        Guid eventId = Guid.NewGuid();
        AgentConversationEndpointResult result = await endpoint.RunAsync(new("project-management.event", "event", eventId.ToString("D"), "show event", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        received.Should().NotBeNull();
        received!.Context.TenantId.Should().Be(tenantId.ToString("D"));
        received.Context.UserId.Should().Be(userId);
        received.Context.EventId.Should().Be(eventId);
        received.Context.ConversationId.Should().BeNull();
    }

    [Fact]
    public async Task Rejects_a_caller_supplied_tenant_identifier()
    {
        Mock<IRequestContext> requestContext = RequestContext(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        Mock<ISender> sender = new(MockBehavior.Strict);
        ProjectManagementAgentConversationEndpoint endpoint = new(requestContext.Object, Member().Object, Access().Object, sender.Object);

        AgentConversationEndpointResult result = await endpoint.RunAsync(new("project-management.event", "event", "invalid", "show event", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.NotFound);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Rejects_a_user_without_current_tenant_membership()
    {
        Mock<IRequestContext> requestContext = RequestContext(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        Mock<IUsersService> users = new();
        users.Setup(value => value.CheckUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<ISender> sender = new(MockBehavior.Strict);
        ProjectManagementAgentConversationEndpoint endpoint = new(requestContext.Object, users.Object, Access().Object, sender.Object);

        AgentConversationEndpointResult result = await endpoint.RunAsync(new("project-management.event", "event", Guid.NewGuid().ToString("D"), "show event", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.NotFound);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Rejects_a_member_without_current_event_access()
    {
        Mock<IRequestContext> requestContext = RequestContext(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        Mock<ISender> sender = new(MockBehavior.Strict);
        Mock<ISeaManagementAccessEvaluator> access = new();
        access.Setup(value => value.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ProjectManagementAgentConversationEndpoint endpoint = new(requestContext.Object, Member().Object, access.Object, sender.Object);

        AgentConversationEndpointResult result = await endpoint.RunAsync(new("project-management.event", "event", Guid.NewGuid().ToString("D"), "show event", null), CancellationToken.None);

        result.Outcome.Should().Be(AgentConversationOutcome.NotFound);
        sender.VerifyNoOtherCalls();
    }

    private static Mock<IRequestContext> RequestContext(Guid tenantId, string userId)
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.TenantId).Returns(tenantId.ToString("D"));
        context.SetupGet(value => value.UserId).Returns(userId);
        return context;
    }

    private static Mock<IUsersService> Member()
    {
        Mock<IUsersService> users = new();
        users.Setup(value => value.CheckUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return users;
    }
    private static Mock<ISeaManagementAccessEvaluator> Access() { Mock<ISeaManagementAccessEvaluator> access = new(); access.Setup(value => value.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true); return access; }
}
