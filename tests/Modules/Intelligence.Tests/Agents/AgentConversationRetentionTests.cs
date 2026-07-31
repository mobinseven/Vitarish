using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.BackgroundJobs;

namespace Intelligence.Tests.Agents;

public sealed class AgentConversationRetentionTests
{
    [Fact]
    public async Task Cleanup_UsesConfiguredCutoffAndReportsDeletedCount()
    {
        DateTimeOffset now = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider clock = new(now);
        Mock<IAgentConversationStore> store = new();
        store.Setup(x => x.DeleteExpiredAsync(now.AddDays(-30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        AgentConversationRetentionCleanup cleanup = new(
            store.Object,
            Options.Create(new AgentConversationRetentionOptions { RetentionDays = 30 }),
            clock,
            NullLogger<AgentConversationRetentionCleanup>.Instance);

        await cleanup.RunAsync();

        store.Verify(x => x.DeleteExpiredAsync(now.AddDays(-30), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OwnerDeletion_RejectsEmptyScope()
    {
        Mock<IAgentConversationStore> store = new();
        DeleteAgentConversationsForOwnerHandler handler = new(store.Object);

        Func<Task> act = () => handler.Handle(new(new()), default);

        await act.Should().ThrowAsync<ArgumentException>();
        store.Verify(x => x.DeleteForOwnerAsync(It.IsAny<AgentConversationOwnerScope>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
