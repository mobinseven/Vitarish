using Vitarish.Intelligence.Application.Agents;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Intelligence.Infrastructure.Services.Agents;

public sealed class AgentRuntimeReplayGuard(
    TimeProvider timeProvider,
    IOperationReceiptStore<string> claims) : IAgentRuntimeReplayGuard
{
    public async Task<bool> TryClaimAsync(
        AgentRuntimeExecutionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (string.IsNullOrWhiteSpace(envelope.ExecutionId) ||
            string.IsNullOrWhiteSpace(envelope.ToolCallId) ||
            string.IsNullOrWhiteSpace(envelope.CallbackSignature) ||
            string.IsNullOrWhiteSpace(envelope.TenantId) ||
            envelope.ExpiresAt <= now)
            return false;

        string claimKey = string.Join(':', envelope.ExecutionId, envelope.ToolCallId, envelope.CallbackSignature);
        return await claims.TryClaimAsync(
            new OperationReceiptKey("agent-runtime-callback", envelope.TenantId, claimKey),
            "claimed",
            envelope.ExpiresAt - now,
            cancellationToken).ConfigureAwait(false);
    }
}
