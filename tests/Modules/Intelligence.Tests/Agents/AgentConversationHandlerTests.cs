using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;

namespace Intelligence.Tests.Agents;

public sealed class AgentConversationHandlerTests
{
    private static readonly AgentConversationScope Scope =
        new("tenant-1", "user-1", "clinic.patient", "patient", "patient-1");
    private static readonly AgentDefinition Definition = new("model", "instructions");
    private static readonly AgentExecutionContext Context = new("execution-1");
    private static readonly AgentExecutionLimits Limits = new(Timeout: TimeSpan.FromSeconds(5));

    [Fact]
    public async Task Handle_CreatesConversationAndAtomicallyReplacesCompleteHistory()
    {
        FakeStore store = new() { History = "[{\"role\":\"assistant\",\"content\":\"prior\"}]" };
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0)
        {
            History = "[{\"role\":\"assistant\",\"content\":\"complete\"}]"
        });
        RunAgentConversationHandler handler = new(store, runner);

        RunAgentConversationResult result = await handler.Handle(Request(null), default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        result.ConversationId.Should().Be(store.ConversationId);
        runner.Input!.History.Should().Be(store.History);
        store.ReplacedHistory.Should().Be(runner.Result.History);
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PreservesPriorHistoryWhenProviderFails()
    {
        FakeStore store = new() { History = "[{\"role\":\"user\",\"content\":\"prior\"}]" };
        FakeRunner runner = new(new AgentRunResult(
            false, null, null, new(0, 0), TimeSpan.Zero, 0, 0,
            new(AgentRunErrorCode.ProviderFailure, "failed")));
        RunAgentConversationHandler handler = new(store, runner);

        RunAgentConversationResult result = await handler.Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.ProviderFailure);
        store.ReplacedHistory.Should().BeNull();
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsBusyWithoutCallingProvider()
    {
        FakeStore store = new() { AcquireOutcome = AgentConversationOutcome.Busy };
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0));

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.Busy);
        runner.Input.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PreservesHistoryAndReleasesLeaseWhenRuntimeIsCancelled()
    {
        FakeStore store = new() { History = "[{\"role\":\"user\",\"content\":\"prior\"}]" };
        FakeRunner runner = new(new AgentRunResult(
            false, null, null, new(0, 0), TimeSpan.Zero, 0, 0,
            new(AgentRunErrorCode.Cancelled, "cancelled")));

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.ProviderFailure);
        store.ReplacedHistory.Should().BeNull();
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RejectsSuccessfulRuntimeResultWithoutCompleteHistory()
    {
        FakeStore store = new() { History = "[]" };
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0));

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.InvalidHistory);
        store.ReplacedHistory.Should().BeNull();
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PropagatesAtomicReplacementConflict()
    {
        FakeStore store = new() { ReplaceOutcome = AgentConversationOutcome.Busy };
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0)
        {
            History = "[]"
        });

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.Busy);
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_UsesOwnedStoreBoundary()
    {
        FakeStore store = new() { DeleteOutcome = AgentConversationOutcome.NotFound };

        AgentConversationOutcome result = await new DeleteAgentConversationHandler(store)
            .Handle(new DeleteAgentConversation(store.ConversationId, Scope), default);

        result.Should().Be(AgentConversationOutcome.NotFound);
        store.DeletedScope.Should().Be(Scope);
    }

    private static RunAgentConversation Request(Guid? conversationId) => new(
        conversationId,
        Scope,
        Definition,
        new AgentInput("new input"),
        [],
        Context,
        Limits);

    private sealed class FakeRunner(AgentRunResult result) : IAgentRunner
    {
        public AgentRunResult Result { get; } = result;
        public AgentInput? Input { get; private set; }

        public Task<AgentRunResult> RunAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            CancellationToken cancellationToken = default)
        {
            Input = input;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeStore : IAgentConversationStore
    {
        public Guid ConversationId { get; } = Guid.CreateVersion7();
        public string? History { get; init; }
        public AgentConversationOutcome AcquireOutcome { get; init; } = AgentConversationOutcome.Succeeded;
        public AgentConversationOutcome ReplaceOutcome { get; init; } = AgentConversationOutcome.Succeeded;
        public AgentConversationOutcome DeleteOutcome { get; init; } = AgentConversationOutcome.Succeeded;
        public string? ReplacedHistory { get; private set; }
        public bool Released { get; private set; }
        public AgentConversationScope? DeletedScope { get; private set; }

        public Task<AgentConversationStoreResult> AcquireAsync(
            Guid? conversationId,
            AgentConversationScope scope,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) =>
            Task.FromResult(AcquireOutcome == AgentConversationOutcome.Succeeded
                ? new AgentConversationStoreResult(AcquireOutcome, new(ConversationId, Guid.CreateVersion7(), 2, History))
                : new AgentConversationStoreResult(AcquireOutcome));

        public Task<AgentConversationOutcome> ReplaceHistoryAsync(
            AgentConversationLease lease,
            AgentConversationScope scope,
            string history,
            string historyFormat,
            CancellationToken cancellationToken)
        {
            ReplacedHistory = history;
            return Task.FromResult(ReplaceOutcome);
        }

        public Task ReleaseAsync(AgentConversationLease lease, CancellationToken cancellationToken)
        {
            Released = true;
            return Task.CompletedTask;
        }

        public Task<AgentConversationOutcome> DeleteAsync(
            Guid conversationId,
            AgentConversationScope scope,
            CancellationToken cancellationToken)
        {
            DeletedScope = scope;
            return Task.FromResult(DeleteOutcome);
        }
    }
}
