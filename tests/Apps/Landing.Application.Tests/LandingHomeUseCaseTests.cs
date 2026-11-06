using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Landing.Application;
using Vitarish.Parties.Application.Apps;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Vitarish.Landing.Application.Tests;

public sealed class LandingHomeUseCaseTests
{
    [Fact]
    public async Task List_returns_null_and_does_not_read_workspaces_when_user_is_missing()
    {
        Mock<IWorkspaceDirectory> workspaces = new(MockBehavior.Strict);
        Mock<IRequestContext> requestContext = new();
        ListLandingWorkspacesQueryHandler handler = new(
            workspaces.Object,
            new PlatformAppCatalog([]),
            requestContext.Object,
            Options.Create(OnboardingOptions()),
            null);

        LandingHomeDirectory? result = await handler.Handle(new ListLandingWorkspacesQuery(), CancellationToken.None);

        result.Should().BeNull();
        workspaces.Verify(directory => directory.ListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_marks_only_apps_matching_the_active_profile_createable()
    {
        const string appId = "clinic-app";
        SelfServiceTenantProfile activeProfile = new(appId, ["dentistry"]);
        PlatformApp[] apps =
        [
            new(appId, "Clinic", PlatformAppAvailability.Live, true, activeProfile),
            new("soon-app", "Soon", PlatformAppAvailability.Soon, true, new SelfServiceTenantProfile("soon-app", ["dentistry"])),
            new("profile-mismatch-app", "Profile mismatch", PlatformAppAvailability.Live, true, new SelfServiceTenantProfile("profile-mismatch-app", ["dermatology"])),
            new("no-self-service-app", "No self-service", PlatformAppAvailability.Live, true, null)
        ];
        Guid tenantId = Guid.NewGuid();
        Mock<IWorkspaceDirectory> workspaces = new();
        workspaces.Setup(directory => directory.ListAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<WorkspaceDirectoryEntry>)[new(tenantId, appId, "My clinic", "clinic.example.test", "owner", true)]);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns("user-1");
        ListLandingWorkspacesQueryHandler handler = new(
            workspaces.Object,
            new PlatformAppCatalog(apps),
            requestContext.Object,
            Options.Create(OnboardingOptions()),
            activeProfile);

        LandingHomeDirectory? result = await handler.Handle(new ListLandingWorkspacesQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Workspaces.Should().ContainSingle().Which.Should().Be(
            new LandingHomeWorkspace(tenantId, appId, "My clinic", "clinic.example.test", true));
        result.Apps.Should().Equal(
            new LandingHomeApp(appId, "Clinic", PlatformAppAvailability.Live, true),
            new LandingHomeApp("soon-app", "Soon", PlatformAppAvailability.Soon, false),
            new LandingHomeApp("profile-mismatch-app", "Profile mismatch", PlatformAppAvailability.Live, false),
            new LandingHomeApp("no-self-service-app", "No self-service", PlatformAppAvailability.Live, false));
    }

    [Fact]
    public async Task Workspace_creation_availability_returns_true_for_a_live_app_matching_the_active_profile()
    {
        const string appId = "clinic-app";
        SelfServiceTenantProfile profile = new(appId, ["dentistry"]);
        IsLandingWorkspaceCreationAvailableQueryHandler handler = new(
            new PlatformAppCatalog([new(appId, "Clinic", PlatformAppAvailability.Live, true, profile)]),
            Options.Create(OnboardingOptions()),
            profile);

        bool available = await handler.Handle(new IsLandingWorkspaceCreationAvailableQuery(appId), CancellationToken.None);

        available.Should().BeTrue();
    }

    [Fact]
    public async Task Workspace_creation_availability_returns_false_when_onboarding_is_disabled()
    {
        const string appId = "clinic-app";
        SelfServiceTenantProfile profile = new(appId, ["dentistry"]);
        IsLandingWorkspaceCreationAvailableQueryHandler handler = new(
            new PlatformAppCatalog([new(appId, "Clinic", PlatformAppAvailability.Live, true, profile)]),
            Options.Create(OnboardingOptions(available: false)),
            profile);

        bool available = await handler.Handle(new IsLandingWorkspaceCreationAvailableQuery(appId), CancellationToken.None);

        available.Should().BeFalse();
    }

    [Fact]
    public async Task Workspace_creation_availability_returns_false_when_the_app_is_not_in_the_catalog()
    {
        IsLandingWorkspaceCreationAvailableQueryHandler handler = new(
            new PlatformAppCatalog([]),
            Options.Create(OnboardingOptions()),
            null);

        bool available = await handler.Handle(new IsLandingWorkspaceCreationAvailableQuery("unknown-app"), CancellationToken.None);

        available.Should().BeFalse();
    }

    [Fact]
    public async Task Workspace_handoff_returns_null_for_a_tenant_outside_the_users_directory()
    {
        Mock<IWorkspaceDirectory> workspaces = new();
        workspaces.Setup(directory => directory.ListAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<WorkspaceDirectoryEntry>)[new(Guid.NewGuid(), "clinic-app", "Mine", "mine.example.test", "owner", false)]);
        Mock<ITenantHandoffIssuer> handoffs = new(MockBehavior.Strict);
        Mock<IRequestContext> requestContext = RequestContext("user-1");
        IssueLandingWorkspaceHandoffCommandHandler handler = new(workspaces.Object, handoffs.Object, requestContext.Object);

        LandingWorkspaceHandoff? result = await handler.Handle(new IssueLandingWorkspaceHandoffCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
        handoffs.Verify(issuer => issuer.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Workspace_handoff_uses_a_non_ending_ticket_for_the_selected_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IWorkspaceDirectory> workspaces = new();
        workspaces.Setup(directory => directory.ListAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<WorkspaceDirectoryEntry>)[new(tenantId, "clinic-app", "Mine", "mine.example.test", "owner", false)]);
        Mock<ITenantHandoffIssuer> handoffs = new();
        handoffs.Setup(issuer => issuer.IssueAsync("user-1", "mine.example.test", It.IsAny<CancellationToken>())).ReturnsAsync("one-time-token");
        IssueLandingWorkspaceHandoffCommandHandler handler = new(workspaces.Object, handoffs.Object, RequestContext("user-1").Object);

        LandingWorkspaceHandoff? result = await handler.Handle(new IssueLandingWorkspaceHandoffCommand(tenantId), CancellationToken.None);

        result.Should().Be(new LandingWorkspaceHandoff("mine.example.test", "one-time-token"));
        handoffs.Verify(issuer => issuer.IssueAsync("user-1", "mine.example.test", It.IsAny<CancellationToken>()), Times.Once);
        handoffs.Verify(issuer => issuer.IssueEndingSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Home_conversation_keeps_create_workspace_action_for_one_eligible_app_id()
    {
        const string appId = "clinic-app";
        SelfServiceTenantProfile profile = new(appId, ["dentistry"]);
        Guid conversationId = Guid.NewGuid();
        RunLandingHomeConversationHandler handler = HomeConversationHandler(
            new PlatformAppCatalog([new(appId, "Clinic", PlatformAppAvailability.Live, true, profile)]),
            [],
            profile,
            new RunAgentConversationResult(AgentConversationOutcome.Succeeded, conversationId, Run("Set up a clinic", LandingHomeAgentValues.CreateWorkspaceActionKey, appId)));

        LandingHomeConversationResult result = await handler.Handle(
            new RunLandingHomeConversationCommand(Guid.NewGuid().ToString("D"), "Set up a workspace", null),
            CancellationToken.None);

        result.Action.Should().Be(new LandingHomeAction(LandingHomeAgentValues.CreateWorkspaceActionKey, AppId: appId));
        result.Reply!.NextAction.Should().Be(LandingHomeAgentValues.CreateWorkspaceActionKey);
    }

    [Fact]
    public async Task Home_conversation_strips_open_workspace_action_for_a_tenant_outside_the_directory()
    {
        Guid currentTenantId = Guid.NewGuid();
        WorkspaceDirectoryEntry currentWorkspace = new(currentTenantId, "clinic-app", "Mine", "mine.example.test", "owner", false);
        Guid foreignTenantId = Guid.NewGuid();
        Guid conversationId = Guid.NewGuid();
        RunLandingHomeConversationHandler handler = HomeConversationHandler(
            new PlatformAppCatalog([]),
            [currentWorkspace],
            null,
            new RunAgentConversationResult(AgentConversationOutcome.Succeeded, conversationId, Run("Open a workspace", LandingHomeAgentValues.OpenWorkspaceActionKey, foreignTenantId.ToString("D"))));

        LandingHomeConversationResult result = await handler.Handle(
            new RunLandingHomeConversationCommand(Guid.NewGuid().ToString("D"), "Open a workspace", conversationId),
            CancellationToken.None);

        result.Action.Should().BeNull();
        result.Reply.Should().NotBeNull();
        result.Reply!.References.Should().BeEmpty();
        result.Reply.NextAction.Should().BeNull();
    }

    [Fact]
    public async Task Delete_home_conversation_binds_scope_to_the_current_user_and_landing_tenant()
    {
        string userId = "user-1";
        Guid tenantId = Guid.NewGuid();
        Guid conversationId = Guid.NewGuid();
        DeleteAgentConversation? forwarded = null;
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<DeleteAgentConversation>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => forwarded = (DeleteAgentConversation)request)
            .ReturnsAsync(AgentConversationOutcome.Busy);
        DeleteLandingHomeConversationCommandHandler handler = new(sender.Object, RequestContext(userId).Object);

        AgentConversationOutcome outcome = await handler.Handle(
            new DeleteLandingHomeConversationCommand(tenantId.ToString("D"), conversationId),
            CancellationToken.None);

        outcome.Should().Be(AgentConversationOutcome.Busy);
        forwarded.Should().NotBeNull();
        forwarded!.ConversationId.Should().Be(conversationId);
        forwarded.Scope.Should().Be(new AgentConversationScope(
            tenantId.ToString("D"),
            userId,
            LandingHomeAgentValues.AgentKey,
            AgentRuntimeSubjectKinds.PlatformHome,
            userId));
    }

    [Fact]
    public async Task Delete_home_conversation_returns_not_found_without_a_signed_in_user()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        Mock<IRequestContext> requestContext = new();
        DeleteLandingHomeConversationCommandHandler handler = new(sender.Object, requestContext.Object);

        AgentConversationOutcome outcome = await handler.Handle(
            new DeleteLandingHomeConversationCommand(Guid.NewGuid().ToString("D"), Guid.NewGuid()),
            CancellationToken.None);

        outcome.Should().Be(AgentConversationOutcome.NotFound);
    }

    private static RunLandingHomeConversationHandler HomeConversationHandler(
        IPlatformAppCatalog apps,
        IReadOnlyList<WorkspaceDirectoryEntry> workspaces,
        SelfServiceTenantProfile? activeProfile,
        RunAgentConversationResult result)
    {
        const string userId = "user-1";
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.IsAny<RunAgentConversation>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        Mock<IWorkspaceDirectory> workspaceDirectory = new();
        workspaceDirectory.Setup(directory => directory.ListAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(workspaces);
        return new(
            sender.Object,
            apps,
            workspaceDirectory.Object,
            RequestContext(userId).Object,
            Options.Create(new LandingConversationAgentOptions { Model = "test-model" }),
            Options.Create(OnboardingOptions()),
            activeProfile);
    }

    private static Mock<IRequestContext> RequestContext(string? userId)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns(userId);
        return requestContext;
    }

    private static OnboardingHostOptions OnboardingOptions(bool available = true) => new()
    {
        OnboardingUrl = "https://onboarding.example.test",
        TenantOnboardingEnabled = available
    };

    private static AgentRunResult Run(string text, string nextAction, params string[] references)
    {
        string json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [AgentInteractiveReplyFormat.TextProperty] = text,
            [AgentInteractiveReplyFormat.QuestionProperty] = null,
            [AgentInteractiveReplyFormat.ReferencesProperty] = references,
            [AgentInteractiveReplyFormat.NextActionProperty] = nextAction
        });
        using JsonDocument document = JsonDocument.Parse(json);
        return new AgentRunResult(true, json, document.RootElement.Clone(), new(1, 1), TimeSpan.Zero, 1, 0);
    }
}
