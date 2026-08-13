using FluentAssertions;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.ProjectManagement.Api;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Users.Application.Abstractions;
using Xunit;

namespace Vitarish.ProjectManagement.Api.Tests;

public sealed class ProjectManagementRuntimeToolEndpointTests
{
    [Fact]
    public async Task Rejects_a_callback_when_current_tenant_membership_was_revoked()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IUsersService> users = new();
        users.Setup(service => service.CheckUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        ProjectManagementRuntimeToolEndpoint endpoint = new(tool.Object, users.Object, Access(false).Object, Writer().Object);

        Func<Task> execute = () => endpoint.ExecuteAsync(Request(tenantId), CancellationToken.None);

        await execute.Should().ThrowAsync<UnauthorizedAccessException>();
        tool.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Dispatches_only_the_sea_management_tool_for_a_current_member()
    {
        Guid tenantId = Guid.NewGuid();
        AgentRuntimeToolEndpointRequest request = Request(tenantId);
        string userId = request.ExecutionContext.Metadata!["authenticatedUserId"];
        Guid eventId = Guid.Parse(request.ExecutionContext.Metadata!["subjectId"]);
        Mock<IUsersService> users = new();
        users.Setup(service => service.CheckUserRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<ISeaManagementAgentTool> tool = new();
        tool.SetupGet(value => value.Definition).Returns(new AgentToolDefinition("sea_manage", "", "{}"));
        tool.Setup(value => value.ExecuteAsync(It.IsAny<AgentToolCall>(), It.IsAny<AgentExecutionContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AgentToolResult("call", "{}"));
        Mock<IRequestContextWriter> requestContextWriter = new();
        List<string> callbackOrder = [];
        requestContextWriter.Setup(value => value.SetContext(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Callback(() => callbackOrder.Add("context"));
        Mock<ISeaManagementAccessEvaluator> access = Access();
        access.Setup(value => value.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callbackOrder.Add("sea"))
            .ReturnsAsync(true);
        ProjectManagementRuntimeToolEndpoint endpoint = new(tool.Object, users.Object, access.Object, requestContextWriter.Object);

        AgentToolResult result = await endpoint.ExecuteAsync(request, CancellationToken.None);

        result.Output.Should().Be("{}");
        callbackOrder.Should().ContainInOrder("context", "sea");
        requestContextWriter.Verify(value => value.SetContext(
            "vitarish-agent-runtime",
            "agent-runtime",
            userId,
            tenantId.ToString("D"),
            userId,
            eventId.ToString("D"),
            request.ExecutionContext.ExecutionId,
            request.ToolCall.Id,
            "Signed project-management event-agent tool callback.",
            null), Times.Once);
    }

    [Fact]
    public async Task Rejects_callback_metadata_for_a_different_subject()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IUsersService> users = new(MockBehavior.Strict);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        Mock<IRequestContextWriter> requestContextWriter = new(MockBehavior.Strict);
        ProjectManagementRuntimeToolEndpoint endpoint = new(tool.Object, users.Object, Access().Object, requestContextWriter.Object);
        AgentRuntimeToolEndpointRequest request = Request(tenantId) with
        {
            ExecutionContext = new AgentExecutionContext("execution", new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString("D"),
                ["authenticatedUserId"] = Guid.NewGuid().ToString("D"),
                ["agentKey"] = "project-management.event",
                ["subjectKind"] = "tenant",
                ["subjectId"] = Guid.NewGuid().ToString("D")
            })
        };

        Func<Task> execute = () => endpoint.ExecuteAsync(request, CancellationToken.None);

        await execute.Should().ThrowAsync<ArgumentException>();
        users.VerifyNoOtherCalls();
        tool.VerifyNoOtherCalls();
        requestContextWriter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Registers_only_event_callbacks_and_rejects_non_event_tenant_metadata()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IUsersService> users = new(MockBehavior.Strict);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        Mock<IRequestContextWriter> requestContextWriter = new(MockBehavior.Strict);
        ProjectManagementRuntimeToolEndpoint endpoint = new(tool.Object, users.Object, Access().Object, requestContextWriter.Object);
        AgentRuntimeToolEndpointRequest request = Request(tenantId) with
        {
            ExecutionContext = new AgentExecutionContext("execution", new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString("D"),
                ["authenticatedUserId"] = Guid.NewGuid().ToString("D"),
                ["agentKey"] = "unrecognized-agent",
                ["subjectKind"] = "tenant",
                ["subjectId"] = tenantId.ToString("D")
            })
        };

        endpoint.AgentKeys.Should().Equal("project-management.event");
        Func<Task> execute = () => endpoint.ExecuteAsync(request, CancellationToken.None);

        await execute.Should().ThrowAsync<ArgumentException>();
        users.VerifyNoOtherCalls();
        tool.VerifyNoOtherCalls();
        requestContextWriter.VerifyNoOtherCalls();
    }

    private static AgentRuntimeToolEndpointRequest Request(Guid tenantId)
    {
        string userId = Guid.NewGuid().ToString("D");
        string eventId = Guid.NewGuid().ToString("D");
        AgentExecutionContext context = new("execution", new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString("D"),
            ["authenticatedUserId"] = userId,
            ["agentKey"] = "project-management.event",
            ["subjectKind"] = "event",
            ["subjectId"] = eventId
        });
        return new(
            new AgentToolCall("call", "sea_manage", "{}"),
            context,
            new AgentRuntimeExecutionEnvelope(
                AgentRuntimeExecutionEnvelope.CurrentVersion,
                context.ExecutionId,
                "project-management.event",
                tenantId.ToString("D"),
                tenantId.ToString("D"),
                "event",
                eventId,
                userId,
                "conversation",
                "call",
                "sea_manage",
                AgentRuntimeExecutionEnvelope.CurrentCredentialVersion,
                DateTimeOffset.UtcNow.AddMinutes(5),
                "correlation",
                "TenantUser",
                "en-US",
                "UTC",
                "gregory",
                DateTimeOffset.UtcNow.ToString("O"),
                DateTimeOffset.UtcNow.ToString("O"),
                []));
    }

    private static Mock<ISeaManagementAccessEvaluator> Access(bool allowed = true) { Mock<ISeaManagementAccessEvaluator> access = new(); access.Setup(value => value.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(allowed); return access; }
    private static Mock<IRequestContextWriter> Writer() => new();
}
