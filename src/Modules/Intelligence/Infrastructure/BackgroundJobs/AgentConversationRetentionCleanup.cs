using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.BackgroundJobs;

public sealed class AgentConversationRetentionCleanup(
    IAgentConversationStore store,
    IOptions<AgentConversationRetentionOptions> options,
    TimeProvider timeProvider,
    ILogger<AgentConversationRetentionCleanup> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        int retentionDays = Math.Max(1, options.Value.RetentionDays);
        int deleted = await store.DeleteExpiredAsync(
            timeProvider.GetUtcNow().AddDays(-retentionDays), cancellationToken);
        logger.LogInformation("Agent conversation retention cleanup removed {ConversationCount} expired records.", deleted);
    }
}
