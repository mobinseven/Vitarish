using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Testing;

namespace Intelligence.Tests.Infrastructure;

public sealed class EfAgentConversationStoreTests
{
    private const string TenantId = "tenant-1";
    private static readonly AgentConversationScope Scope = new(TenantId, "user-1", "clinic.assistant.operations", "practice", "practice");
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(1);

    [DockerFact]
    public async Task A_conversation_continues_after_its_configuration_changes_and_records_the_new_key_with_the_history()
    {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();
        await using IntelligenceDbContext context = await CreateContextAsync(postgres.GetConnectionString());
        EfAgentConversationStore store = new(context, new EphemeralDataProtectionProvider(), TimeProvider.System);

        AgentConversationStoreResult created = await store.AcquireAsync(null, Scope, Lease, "key-a", CancellationToken.None);
        (await store.ReplaceHistoryAsync(created.Lease!, Scope, "[\"first\"]", RunAgentConversationHandler.HistoryFormat, CancellationToken.None))
            .Should().Be(AgentConversationOutcome.Succeeded);

        AgentConversationStoreResult changed = await store.AcquireAsync(created.Lease!.ConversationId, Scope, Lease, "key-b", CancellationToken.None);

        changed.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        changed.Lease!.ProfileChanged.Should().BeTrue();
        changed.Lease.History.Should().Be("[\"first\"]");
        (await store.ReplaceHistoryAsync(changed.Lease, Scope, "[\"second\"]", RunAgentConversationHandler.HistoryFormat, CancellationToken.None))
            .Should().Be(AgentConversationOutcome.Succeeded);

        AgentConversationStoreResult next = await store.AcquireAsync(created.Lease.ConversationId, Scope, Lease, "key-b", CancellationToken.None);
        next.Lease!.ProfileChanged.Should().BeFalse();
        next.Lease.History.Should().Be("[\"second\"]");
        (await context.AgentConversations.AsNoTracking().SingleAsync()).ProfileCompatibilityKey.Should().Be("key-b");
    }

    [DockerFact]
    public async Task A_failed_turn_after_a_configuration_change_keeps_the_previous_key_so_the_next_turn_carries_over_again()
    {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();
        await using IntelligenceDbContext context = await CreateContextAsync(postgres.GetConnectionString());
        EfAgentConversationStore store = new(context, new EphemeralDataProtectionProvider(), TimeProvider.System);
        AgentConversationStoreResult created = await store.AcquireAsync(null, Scope, Lease, "key-a", CancellationToken.None);
        await store.ReplaceHistoryAsync(created.Lease!, Scope, "[\"first\"]", RunAgentConversationHandler.HistoryFormat, CancellationToken.None);

        AgentConversationStoreResult changed = await store.AcquireAsync(created.Lease!.ConversationId, Scope, Lease, "key-b", CancellationToken.None);
        await store.ReleaseAsync(changed.Lease!, CancellationToken.None);

        AgentConversationStoreResult retried = await store.AcquireAsync(created.Lease.ConversationId, Scope, Lease, "key-b", CancellationToken.None);
        retried.Lease!.ProfileChanged.Should().BeTrue();
        retried.Lease.History.Should().Be("[\"first\"]");
    }

    [DockerFact]
    public async Task A_conversation_saved_without_a_key_continues_under_a_keyed_configuration()
    {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync();
        await using IntelligenceDbContext context = await CreateContextAsync(postgres.GetConnectionString());
        EfAgentConversationStore store = new(context, new EphemeralDataProtectionProvider(), TimeProvider.System);
        AgentConversationStoreResult legacy = await store.AcquireAsync(null, Scope, Lease, CancellationToken.None);
        await store.ReplaceHistoryAsync(legacy.Lease!, Scope, "[\"legacy\"]", RunAgentConversationHandler.HistoryFormat, CancellationToken.None);

        AgentConversationStoreResult keyed = await store.AcquireAsync(legacy.Lease!.ConversationId, Scope, Lease, "key-a", CancellationToken.None);

        keyed.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        keyed.Lease!.ProfileChanged.Should().BeTrue();
    }

    // Production enables the retrying execution strategy; the store must work under it.
    private static async Task<IntelligenceDbContext> CreateContextAsync(string connectionString)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.TenantId).Returns(TenantId);
        DbContextOptions<IntelligenceDbContext> options = new DbContextOptionsBuilder<IntelligenceDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .Options;
        IntelligenceDbContext context = new(options, [], requestContext.Object, new HttpContextAccessor());
        await context.Database.MigrateAsync();
        return context;
    }
}
