using FluentAssertions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Contracts.ClinicAssistant;
using Vitarish.Clinic.Contracts.Routes;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class ClinicAssistantTests
{
    [Fact]
    public void Patient_id_is_route_bound_and_not_part_of_the_request_body()
    {
        typeof(RunClinicAssistantRequest).GetProperties().Select(property => property.Name)
            .Should().NotContain("PatientId");
        ClinicApiRoutes.PatientAssistant.Should().Contain("{patientId:guid}");
    }

    [Fact]
    public async Task Handler_returns_runtime_actions_without_persisting_after_history_commit()
    {
        Guid proposalId = Guid.NewGuid();
        RecordingRuntime runtime = new(new AssistantResult(
            "run-1",
            "conversation-1",
            [],
            [])
        {
            PreparedActions =
            [
                new AssistantPreparedAction(
                    "Appointments",
                    "create_appointment",
                    proposalId,
                    "{\"proposalId\":\"server-derived\"}",
                    DateTimeOffset.Parse("2030-01-01T00:00:00Z"),
                    "Review appointment",
                    "tool-call-1",
                    "prepare_appointment")
            ]
        });
        RecordingActionStore store = new();
        AssistantContext context = Context();

        AssistantResult result = await new RunAssistantHandler(runtime)
            .Handle(new RunAssistant(context, new AssistantInput("Prepare the appointment")), CancellationToken.None);

        result.PreparedActions.Should().ContainSingle().Which.OwningId.Should().Be(proposalId);
        store.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Failed_runtime_outcome_does_not_persist_actions()
    {
        RecordingRuntime runtime = new(AssistantResult.Failure("run-1", "conversation-1", "profile changed") with
        {
            Outcome = AssistantRunOutcome.ProfileChanged
        });
        RecordingActionStore store = new();

        AssistantResult result = await new RunAssistantHandler(runtime)
            .Handle(new RunAssistant(Context(), new AssistantInput("Continue")), CancellationToken.None);

        result.Outcome.Should().Be(AssistantRunOutcome.ProfileChanged);
        store.Saved.Should().BeEmpty();
    }

    private static AssistantContext Context() => new(
        "practice-1",
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "user-1",
        new HashSet<string>(StringComparer.Ordinal) { "Appointments.Read" },
        "TenantOwner",
        "en-US",
        "UTC",
        "conversation-1");

    private sealed class RecordingRuntime(AssistantResult result) : IAssistantRuntime
    {
        public Task<AssistantResult> RunAsync(AssistantContext context, AssistantInput input, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private sealed class RecordingActionStore : IAssistantActionStore
    {
        public List<AssistantAction> Saved { get; } = [];
        public Task SaveAsync(AssistantAction action, CancellationToken cancellationToken) { Saved.Add(action); return Task.CompletedTask; }
        public Task<AssistantAction?> GetAsync(Guid actionId, CancellationToken cancellationToken) => Task.FromResult<AssistantAction?>(null);
        public Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> FailAsync(Guid actionId, AssistantActionFailure failure, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

}
