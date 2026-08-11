using FluentAssertions;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.ProjectManagement.Application;
using Vitarish.Sea.Application.Agents;
using Xunit;

namespace Vitarish.ProjectManagement.Application.Tests;

public sealed class ProjectManagementActionWorkflowsTests
{
    [Fact]
    public async Task Completed_action_is_idempotent_without_reexecuting_the_tool()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Completed);
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.Setup(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        ProjectManagementActionWorkflows workflows = new(store.Object, tool.Object, TimeProvider.System);

        ProjectManagementActionResult result = await workflows.ConfirmAsync(action.ActionId, new(action.TenantId, action.RequestedByUserId, action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Succeeded);
        result.Action.Should().Be(action);
        tool.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Action_from_another_user_is_forbidden()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Proposed);
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.Setup(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action);
        ProjectManagementActionWorkflows workflows = new(store.Object, new Mock<ISeaManagementAgentTool>().Object, TimeProvider.System);

        ProjectManagementActionResult result = await workflows.GetAsync(action.ActionId, new(action.TenantId, "other-user", action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Forbidden);
    }

    [Fact]
    public async Task Revoked_sea_access_is_recorded_and_rejected_during_confirmation()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Proposed);
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.SetupSequence(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action).ReturnsAsync(action);
        store.Setup(x => x.TryBeginConfirmationAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.FailAsync(action.ActionId, It.Is<ProjectManagementActionFailure>(failure => failure.Code == "authorization_revoked" && !failure.Retryable), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        tool.Setup(x => x.ExecutePreparedAsync(It.IsAny<AgentToolCall>(), action.PreparedAction, It.IsAny<SeaManagementAgentSubject>(), It.IsAny<CancellationToken>())).ThrowsAsync(new UnauthorizedAccessException());
        ProjectManagementActionWorkflows workflows = new(store.Object, tool.Object, TimeProvider.System);

        ProjectManagementActionResult result = await workflows.ConfirmAsync(action.ActionId, new(action.TenantId, action.RequestedByUserId, action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Forbidden);
    }

    [Fact]
    public async Task Event_confirmation_reattaches_the_original_event_scope_for_reauthorization()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Proposed);
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.SetupSequence(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action).ReturnsAsync(action with { Status = ProjectManagementActionStatus.Completed });
        store.Setup(x => x.TryBeginConfirmationAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.CompleteAsync(action.ActionId, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SeaManagementAgentSubject? receivedSubject = null;
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        tool.Setup(x => x.ExecutePreparedAsync(It.IsAny<AgentToolCall>(), action.PreparedAction, It.IsAny<SeaManagementAgentSubject>(), It.IsAny<CancellationToken>()))
            .Callback<AgentToolCall, SeaManagementPreparedAction, SeaManagementAgentSubject, CancellationToken>((_, _, subject, _) => receivedSubject = subject)
            .ReturnsAsync(new AgentToolResult("call", "{}"));
        ProjectManagementActionResult result = await new ProjectManagementActionWorkflows(store.Object, tool.Object, TimeProvider.System)
            .ConfirmAsync(action.ActionId, new(action.TenantId, action.RequestedByUserId, action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Succeeded);
        receivedSubject.Should().NotBeNull();
        receivedSubject!.EventId.Should().Be(action.EventId);
    }

    [Fact]
    public async Task Expired_action_is_never_executed_and_is_marked_expired()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Proposed) with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) };
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.SetupSequence(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action).ReturnsAsync(action with { Status = ProjectManagementActionStatus.Expired });
        store.Setup(x => x.ExpireAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        ProjectManagementActionWorkflows workflows = new(store.Object, tool.Object, TimeProvider.System);

        ProjectManagementActionResult result = await workflows.ConfirmAsync(action.ActionId, new(action.TenantId, action.RequestedByUserId, action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Expired);
        tool.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Concurrent_confirmation_returns_conflict_without_executing_the_tool()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Proposed);
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.SetupSequence(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action).ReturnsAsync(action with { Status = ProjectManagementActionStatus.Confirming });
        store.Setup(x => x.TryBeginConfirmationAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        ProjectManagementActionWorkflows workflows = new(store.Object, tool.Object, TimeProvider.System);

        ProjectManagementActionResult result = await workflows.ConfirmAsync(action.ActionId, new(action.TenantId, action.RequestedByUserId, action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Conflict);
        tool.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Unknown_mutation_outcome_is_non_retryable()
    {
        ProjectManagementAction action = Action(ProjectManagementActionStatus.Proposed);
        Mock<IProjectManagementActionStore> store = new(MockBehavior.Strict);
        store.SetupSequence(x => x.GetAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(action).ReturnsAsync(action with { Status = ProjectManagementActionStatus.Failed });
        store.Setup(x => x.TryBeginConfirmationAsync(action.ActionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.FailAsync(action.ActionId, It.Is<ProjectManagementActionFailure>(failure => failure.Code == "unknown_commit_outcome" && !failure.Retryable), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<ISeaManagementAgentTool> tool = new(MockBehavior.Strict);
        tool.Setup(x => x.ExecutePreparedAsync(It.IsAny<AgentToolCall>(), action.PreparedAction, It.IsAny<SeaManagementAgentSubject>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
        ProjectManagementActionWorkflows workflows = new(store.Object, tool.Object, TimeProvider.System);

        ProjectManagementActionResult result = await workflows.ConfirmAsync(action.ActionId, new(action.TenantId, action.RequestedByUserId, action.EventId, "en"), CancellationToken.None);

        result.Outcome.Should().Be(ProjectManagementActionOutcome.Failed);
    }

    private static ProjectManagementAction Action(ProjectManagementActionStatus status) => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"), "tenant-1", "user-1", Guid.Parse("33333333-3333-3333-3333-333333333333"), "run-1", "call-1",
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "update_event", "{}", DateTimeOffset.UtcNow.AddMinutes(10)), status,
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10), status == ProjectManagementActionStatus.Completed ? "{}" : null);
}
