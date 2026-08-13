using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Infrastructure.Services.Agents;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Intelligence.Tests.Agents;

public sealed class AgentRuntimeReplayGuardTests
{
    [Fact]
    public async Task TryClaim_AllowsOneSignedToolCallAndRejectsItsReplay()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero));
        InMemoryClaimStore claims = new();
        AgentRuntimeReplayGuard guard = new(time, claims);
        AgentRuntimeExecutionEnvelope envelope = new(
            1, "execution-1", "clinic.assistant.patient", "practice-1", "practice-1", "patient",
            "11111111-1111-1111-1111-111111111111", "user-1", "conversation-1", "call-1", "read_patient", 1,
            time.GetUtcNow().AddMinutes(5), "execution-1", "Clinician", "en-US", "UTC", "gregory",
            time.GetUtcNow().ToString("O"), time.GetUtcNow().ToString("O"), [], CallbackSignature: "signature-1");

        (await guard.TryClaimAsync(envelope)).Should().BeTrue();
        (await guard.TryClaimAsync(envelope)).Should().BeFalse();
        (await guard.TryClaimAsync(envelope with { ToolCallId = "call-2", CallbackSignature = "signature-2" })).Should().BeTrue();
    }

    private sealed class InMemoryClaimStore : IOperationReceiptStore<string>
    {
        private readonly HashSet<OperationReceiptKey> keys = [];

        public Task<OperationReceipt<string>?> GetAsync(OperationReceiptKey key, CancellationToken cancellationToken) =>
            Task.FromResult<OperationReceipt<string>?>(null);

        public Task<bool> TryClaimAsync(OperationReceiptKey key, string value, TimeSpan lifetime, CancellationToken cancellationToken) =>
            Task.FromResult(keys.Add(key));

        public Task<bool> TryUpdateAsync(OperationReceiptKey key, long expectedRevision, string value, TimeSpan lifetime, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
