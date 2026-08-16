using FluentAssertions;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Infrastructure.AgentCapabilities;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Appointments.Tests.AgentCapabilities;

public sealed class PatientAppointmentWorkflowStoreTests
{
    [Fact]
    public async Task Proposals_are_isolated_by_practice_and_cannot_be_claimed_twice()
    {
        ReceiptStore<AppointmentProposal> proposals = new();
        PatientAppointmentWorkflowStore sut = new(proposals, new ReceiptStore<AppointmentCommitReceipt>());
        AppointmentProposal proposal = Proposal("practice-a");

        await sut.SaveProposalAsync(proposal, default);

        (await sut.GetProposalAsync(proposal.Id, "practice-a", default)).Should().Be(proposal);
        (await sut.GetProposalAsync(proposal.Id, "practice-b", default)).Should().BeNull();
        Func<Task> duplicate = () => sut.SaveProposalAsync(proposal, default);
        await duplicate.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Commit_claim_is_single_use_and_terminal_receipts_are_not_overwritten()
    {
        ReceiptStore<AppointmentCommitReceipt> commits = new();
        PatientAppointmentWorkflowStore sut = new(new ReceiptStore<AppointmentProposal>(), commits);
        Guid proposalId = Guid.NewGuid();

        (await sut.TryClaimCommitAsync(proposalId, "practice", default)).Should().BeTrue();
        (await sut.TryClaimCommitAsync(proposalId, "practice", default)).Should().BeFalse();
        (await sut.CompleteCommitAsync(proposalId, "practice", new(AppointmentCommitReceiptStatus.Committed, Guid.NewGuid()), default)).Should().BeTrue();
        (await sut.CompleteCommitAsync(proposalId, "practice", new(AppointmentCommitReceiptStatus.Failed), default)).Should().BeFalse();
        (await sut.GetCommitReceiptAsync(proposalId, "practice", default))!.Status.Should().Be(AppointmentCommitReceiptStatus.Committed);
    }

    [Fact]
    public async Task Commit_finalization_reports_a_failed_compare_and_swap()
    {
        ReceiptStore<AppointmentCommitReceipt> commits = new() { AllowUpdates = false };
        PatientAppointmentWorkflowStore sut = new(new ReceiptStore<AppointmentProposal>(), commits);
        Guid proposalId = Guid.NewGuid();
        await sut.TryClaimCommitAsync(proposalId, "practice", default);

        (await sut.CompleteCommitAsync(proposalId, "practice", new(AppointmentCommitReceiptStatus.Failed), default)).Should().BeFalse();
        (await sut.GetCommitReceiptAsync(proposalId, "practice", default))!.Status.Should().Be(AppointmentCommitReceiptStatus.InProgress);
    }

    private static AppointmentProposal Proposal(string practiceId) => new(
        Guid.NewGuid(), Guid.NewGuid(), practiceId,
        new(Guid.NewGuid(), new DateOnly(2026, 8, 12), new TimeOnly(9, 0), TimeSpan.FromMinutes(30), Guid.NewGuid(), "Consultation", null, null, "Europe/London"),
        null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10));

    private sealed class ReceiptStore<T> : IOperationReceiptStore<T>
    {
        private readonly Dictionary<OperationReceiptKey, OperationReceipt<T>> receipts = [];
        public bool AllowUpdates { get; init; } = true;

        public Task<OperationReceipt<T>?> GetAsync(OperationReceiptKey key, CancellationToken cancellationToken) =>
            Task.FromResult(receipts.GetValueOrDefault(key));

        public Task<bool> TryClaimAsync(OperationReceiptKey key, T value, TimeSpan lifetime, CancellationToken cancellationToken) =>
            Task.FromResult(receipts.TryAdd(key, new OperationReceipt<T>(0, value)));

        public Task<bool> TryUpdateAsync(OperationReceiptKey key, long expectedRevision, T value, TimeSpan lifetime, CancellationToken cancellationToken)
        {
            if (!AllowUpdates || !receipts.TryGetValue(key, out OperationReceipt<T>? current) || current.Revision != expectedRevision)
                return Task.FromResult(false);

            receipts[key] = new(expectedRevision + 1, value);
            return Task.FromResult(true);
        }
    }
}
