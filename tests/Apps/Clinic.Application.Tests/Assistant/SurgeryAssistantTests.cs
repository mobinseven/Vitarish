using System.Text.Json;
using FluentAssertions;
using Moq;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Clinic.Application.Assistant.Surgeries;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class SurgeryAssistantTests
{
    private const string PracticeId = "practice-1";
    private const string OwnerId = "owner-1";
    private const string RunId = "run-1";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2030-01-10T08:00:00Z");
    private static readonly Guid PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherPatientId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid SurgeryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Token = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly SurgeryCatalog GeneralSurgery = new(
        "55555555-5555-5555-5555-555555555555", "General surgery", ["Scalpel"], ["Surgeon"],
        [new("1 day", 1440, "Tomorrow", "Your surgery is tomorrow.")], Guid.NewGuid());

    private readonly Mock<ISurgeryToolchain> _surgeries = new();
    private readonly Mock<ISurgeryCatalogStore> _catalogs = new();
    private readonly Mock<ISurgerySettingsStore> _settings = new();
    private readonly Mock<ISurgeryTeamDirectory> _directory = new();
    private readonly Mock<IAssistantPatientSearch> _patients = new();
    private readonly InMemoryProposalStore _proposals = new();
    private readonly FixedTimeProvider _clock = new(Now);

    public SurgeryAssistantTests()
    {
        _catalogs.Setup(store => store.FindAsync(GeneralSurgery.Id, It.IsAny<CancellationToken>())).ReturnsAsync(GeneralSurgery);
    }

    [Fact]
    public void Both_capabilities_and_the_committer_register_without_drift()
    {
        AssistantCapabilityRegistry registry = Registry();

        registry.Policies.Select(policy => policy.ActionType).Should().BeEquivalentTo(SurgeryAssistantActions.ActionTypes);
        registry.Policies.Should().OnlyContain(policy => policy.RequiresConfirmation && policy.OwningWorkflow == SurgeryAssistantActions.Workflow);
    }

    [Fact]
    public void Only_the_clinic_owner_is_offered_surgery_changes()
    {
        AssistantCapabilityRegistry registry = Registry();
        AssistantActionPolicy policy = registry.ResolveTool(SurgeryAssistantActions.PrepareCancellationTool)!;
        HashSet<string> permissions = ["ClinicalEvents.Read", "ClinicalEvents.Create"];

        policy.Authorizes(PatientContext(permissions, "Clinician")).Should().BeFalse();
        policy.Authorizes(PatientContext(permissions, AssistantOperationsScope.TenantOwnerRole)).Should().BeTrue();
    }

    [Fact]
    public async Task A_practice_plan_keeps_the_resolved_patient_on_the_server()
    {
        _patients.Setup(search => search.ResolveAsync(PracticeId, OwnerId, RunId, "opaque-candidate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssistantCandidateReference(PatientId, PracticeId, OwnerId, RunId, "Ada Lovelace", "1******9", "0******1"));
        IAssistantPreparationTool tool = Tool(Operations(), SurgeryAssistantActions.OperationsPrepareTool);

        AgentToolResult result = await tool.ExecuteAsync(Call(SurgeryAssistantActions.OperationsPrepareTool,
            $$"""{"candidateToken":"opaque-candidate","catalogId":"{{GeneralSurgery.Id}}","name":"Knee arthroscopy","scheduledAt":"2030-01-12T09:00:00+03:30"}"""),
            Execution(practice: true));
        AssistantPreparedAction action = tool.GetPreparedAction(result)!;

        result.Output.Should().NotContain(PatientId.ToString("D")).And.NotContain(PatientId.ToString("N"));
        action.ReviewDataJson.Should().NotContain(PatientId.ToString("D")).And.Contain("Ada Lovelace");
        action.ActionType.Should().Be(SurgeryAssistantActions.OperationsCreate);
        action.Capability.Should().Be(SurgeryAssistantActions.OperationsCapability);
        SurgeryAssistantProposal proposal = _proposals.Saved.Single();
        proposal.Id.Should().Be(action.OwningId);
        proposal.Surgery!.PatientId.Should().Be(PatientId);
        // What the confirmation writes is what the review shows: the type's tools and reminders are resolved now.
        proposal.Surgery.Tools.Should().Equal(GeneralSurgery.Tools);
        proposal.Surgery.ReminderPolicies.Should().Equal(GeneralSurgery.ReminderPolicies);
        proposal.Surgery.IdempotencyKey.Should().Be(proposal.Id.ToString("N"));
    }

    [Fact]
    public async Task The_patient_assistant_cannot_reach_another_patients_surgery()
    {
        GivenSurgery(Surgery(OtherPatientId, SurgeryStatus.Planned, Now.AddDays(2)));
        IAgentTool tool = Tool(Patient(), SurgeryAssistantActions.PrepareCancellationTool);

        Func<Task> prepare = () => tool.ExecuteAsync(Call(SurgeryAssistantActions.PrepareCancellationTool, $$"""{"surgeryId":"{{SurgeryId}}"}"""), Execution());

        await prepare.Should().ThrowAsync<ArgumentException>().WithMessage("*does not exist*");
        _proposals.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task An_edit_reviews_the_change_and_can_correct_a_mistaken_completion()
    {
        GivenSurgery(Surgery(PatientId, SurgeryStatus.Completed, Now.AddDays(-1)));
        IAssistantPreparationTool tool = Tool(Patient(), SurgeryAssistantActions.PrepareUpdateTool);

        AgentToolResult result = await tool.ExecuteAsync(Call(SurgeryAssistantActions.PrepareUpdateTool,
            $$"""{"surgeryId":"{{SurgeryId}}","status":"planned","location":"OR 3"}"""), Execution());

        using JsonDocument review = JsonDocument.Parse(tool.GetPreparedAction(result)!.ReviewDataJson);
        review.RootElement.GetProperty("previousSurgery").GetProperty("status").GetString().Should().Be("Completed");
        review.RootElement.GetProperty("surgery").GetProperty("status").GetString().Should().Be("Planned");
        review.RootElement.GetProperty("surgery").GetProperty("location").GetString().Should().Be("OR 3");
        SurgeryWriteModel written = _proposals.Saved.Single().Surgery!;
        written.Id.Should().Be(SurgeryId);
        written.ConcurrencyToken.Should().Be(Token);
        written.Status.Should().Be(SurgeryStatus.Planned);
    }

    [Fact]
    public async Task An_edit_that_changes_nothing_is_refused()
    {
        GivenSurgery(Surgery(PatientId, SurgeryStatus.Planned, Now.AddDays(2)));
        IAgentTool tool = Tool(Patient(), SurgeryAssistantActions.PrepareUpdateTool);

        Func<Task> prepare = () => tool.ExecuteAsync(Call(SurgeryAssistantActions.PrepareUpdateTool, $$"""{"surgeryId":"{{SurgeryId}}","location":"OR 1"}"""), Execution());

        await prepare.Should().ThrowAsync<ArgumentException>().WithMessage("*changes nothing*");
    }

    [Fact]
    public async Task Only_a_planned_surgery_can_be_cancelled_and_only_a_started_one_completed()
    {
        GivenSurgery(Surgery(PatientId, SurgeryStatus.Completed, Now.AddDays(-1)));
        Func<Task> cancel = () => Tool(Patient(), SurgeryAssistantActions.PrepareCancellationTool)
            .ExecuteAsync(Call(SurgeryAssistantActions.PrepareCancellationTool, $$"""{"surgeryId":"{{SurgeryId}}"}"""), Execution());
        await cancel.Should().ThrowAsync<ArgumentException>().WithMessage("*Only a planned surgery can be cancelled*");

        GivenSurgery(Surgery(PatientId, SurgeryStatus.Planned, Now.AddHours(2)));
        Func<Task> complete = () => Tool(Patient(), SurgeryAssistantActions.PrepareCompletionTool)
            .ExecuteAsync(Call(SurgeryAssistantActions.PrepareCompletionTool, $$"""{"surgeryId":"{{SurgeryId}}"}"""), Execution());
        await complete.Should().ThrowAsync<InvalidOperationException>().WithMessage("*before its scheduled start*");
    }

    [Fact]
    public async Task A_cancellation_review_warns_that_the_patient_and_team_are_notified()
    {
        GivenSurgery(Surgery(PatientId, SurgeryStatus.Planned, Now.AddDays(2)));
        IAssistantPreparationTool tool = Tool(Operations(), SurgeryAssistantActions.OperationsPrepareCancellationTool);

        AgentToolResult result = await tool.ExecuteAsync(Call(SurgeryAssistantActions.OperationsPrepareCancellationTool, $$"""{"surgeryId":"{{SurgeryId}}"}"""), Execution(practice: true));

        using JsonDocument review = JsonDocument.Parse(tool.GetPreparedAction(result)!.ReviewDataJson);
        review.RootElement.GetProperty("notifiesPatientAndTeam").GetBoolean().Should().BeTrue();
        review.RootElement.GetProperty("patient").GetProperty("displayName").GetString().Should().Be("Ada Lovelace");
        _proposals.Saved.Single().ExpectedConcurrencyToken.Should().Be(Token);
    }

    [Fact]
    public async Task Practice_reads_name_the_patient_without_a_patient_id()
    {
        _surgeries.Setup(toolchain => toolchain.GetCalendarAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SurgeryCalendarEntry(Surgery(PatientId, SurgeryStatus.Planned, Now.AddDays(2)), "Ada Lovelace")]);
        IAgentTool tool = Operations().Tools.Single(item => item.Definition.Name == SurgeryAssistantActions.OperationsListTool);

        AgentToolResult result = await tool.ExecuteAsync(Call(SurgeryAssistantActions.OperationsListTool,
            """{"from":"2030-01-01T00:00:00Z","to":"2030-02-01T00:00:00Z"}"""), Execution(practice: true));

        result.Output.Should().Contain("Ada Lovelace").And.Contain(SurgeryId.ToString("D")).And.NotContain(PatientId.ToString("D"));
    }

    [Fact]
    public async Task A_confirmation_from_another_user_writes_nothing()
    {
        SurgeryAssistantProposal proposal = await PrepareCompletionAsync();
        SurgeryAssistantActionCommitter committer = Committer();

        AssistantActionCommitResult result = await committer.CommitAsync(
            Action(proposal), PatientContext(new HashSet<string>(), AssistantOperationsScope.TenantOwnerRole) with { AuthenticatedUserId = "someone-else" }, CancellationToken.None);

        result.Failure!.Code.Should().Be("owning_proposal_forbidden");
        _surgeries.Verify(toolchain => toolchain.CompleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_confirmation_applies_with_the_token_read_at_preparation()
    {
        SurgeryAssistantProposal proposal = await PrepareCompletionAsync();
        _surgeries.Setup(toolchain => toolchain.CompleteAsync(SurgeryId, Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Surgery(PatientId, SurgeryStatus.Completed, Now.AddHours(-1)));

        AssistantActionCommitResult result = await Committer().CommitAsync(Action(proposal), OwnerContext(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.CompletionPayload.Should().Contain(SurgeryId.ToString("D"));
    }

    [Fact]
    public async Task A_surgery_changed_since_preparation_fails_without_retry()
    {
        SurgeryAssistantProposal proposal = await PrepareCompletionAsync();
        _surgeries.Setup(toolchain => toolchain.CompleteAsync(SurgeryId, Token, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The surgery was changed by another request."));

        AssistantActionCommitResult result = await Committer().CommitAsync(Action(proposal), OwnerContext(), CancellationToken.None);

        result.Failure.Should().BeEquivalentTo(new AssistantActionFailure("surgery_changed", "The surgery was changed by another request.", false));
    }

    [Fact]
    public async Task An_unavailable_dependency_can_be_confirmed_again()
    {
        SurgeryAssistantProposal proposal = await PrepareCompletionAsync();
        _surgeries.Setup(toolchain => toolchain.CompleteAsync(SurgeryId, Token, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SurgeryOperationUnavailableException(new TimeoutException()));

        AssistantActionCommitResult result = await Committer().CommitAsync(Action(proposal), OwnerContext(), CancellationToken.None);

        result.Failure!.Code.Should().Be("surgery_unavailable");
        result.Failure.Retryable.Should().BeTrue();
    }

    private async Task<SurgeryAssistantProposal> PrepareCompletionAsync()
    {
        GivenSurgery(Surgery(PatientId, SurgeryStatus.Planned, Now.AddHours(-1)));
        _ = await Tool(Patient(), SurgeryAssistantActions.PrepareCompletionTool)
            .ExecuteAsync(Call(SurgeryAssistantActions.PrepareCompletionTool, $$"""{"surgeryId":"{{SurgeryId}}"}"""), Execution());
        return _proposals.Saved.Single();
    }

    private void GivenSurgery(SurgeryReadModel surgery) =>
        _surgeries.Setup(toolchain => toolchain.GetAsync(SurgeryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SurgeryCalendarEntry(surgery, "Ada Lovelace"));

    private static SurgeryReadModel Surgery(Guid patientId, SurgeryStatus status, DateTimeOffset scheduledAt) => new(
        SurgeryId, patientId, GeneralSurgery.Id, "Knee arthroscopy", "key-1", status, scheduledAt,
        ["Scalpel"], [], [], [], Token, DateTime.UtcNow, null, 90, "OR 1", []);

    private PatientSurgeryAgentCapability Patient() =>
        new(_surgeries.Object, _catalogs.Object, _directory.Object, _patients.Object, _proposals, _clock);

    private SurgeryOperationsAgentCapability Operations() =>
        new(_surgeries.Object, _catalogs.Object, _settings.Object, _directory.Object, _patients.Object, _proposals, _clock);

    private SurgeryAssistantActionCommitter Committer() =>
        new(_proposals, _surgeries.Object, _catalogs.Object, _settings.Object);

    private AssistantCapabilityRegistry Registry() => new(
        [Patient(), Operations()],
        new AssistantAuthorizationPolicy(),
        new AssistantToolAvailabilityPolicy(),
        [Committer()]);

    private static IAssistantPreparationTool Tool(IAssistantCapability capability, string name) =>
        (IAssistantPreparationTool)capability.Tools.Single(tool => tool.Definition.Name == name);

    private static AgentToolCall Call(string name, string arguments) => new("call-1", name, arguments);

    private static AgentExecutionContext Execution(bool practice = false) => new(RunId, new Dictionary<string, string>
    {
        ["practiceId"] = PracticeId,
        ["authenticatedUserId"] = OwnerId,
        ["patientId"] = (practice ? Guid.Empty : PatientId).ToString("D"),
        ["subjectKind"] = practice ? "practice" : "patient"
    });

    private static AssistantContext PatientContext(IReadOnlySet<string> permissions, string role) =>
        new(PracticeId, PatientId, OwnerId, permissions, role, "en-US", "UTC", "conversation-1");

    private static AssistantContext OwnerContext() =>
        PatientContext(new HashSet<string>(), AssistantOperationsScope.TenantOwnerRole);

    private static AssistantAction Action(SurgeryAssistantProposal proposal) => new(
        Guid.NewGuid(), RunId, "conversation-1", PatientId, PracticeId, OwnerId, SurgeryAssistantActions.Capability,
        proposal.ActionType, proposal.Id, "{}", AssistantActionStatus.Confirming, Now, proposal.ExpiresAt);

    private sealed class InMemoryProposalStore : ISurgeryAssistantProposalStore
    {
        public List<SurgeryAssistantProposal> Saved { get; } = [];

        public Task SaveAsync(SurgeryAssistantProposal proposal, CancellationToken cancellationToken)
        {
            Saved.Add(proposal);
            return Task.CompletedTask;
        }

        public Task<SurgeryAssistantProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.SingleOrDefault(proposal => proposal.Id == proposalId));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
