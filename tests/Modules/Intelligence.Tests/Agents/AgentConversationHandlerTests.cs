using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    public async Task Handle_ValidatesApplicationResultBeforeReplacingHistory()
    {
        FakeStore store = new();
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0)
        {
            History = "[]"
        });
        store.CallbackObserved = false;

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with
            {
                BeforeCommit = (_, _, _) =>
                {
                    store.CallbackObserved = true;
                    return Task.FromResult(AgentConversationOutcome.Succeeded);
                }
            }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        store.CallbackObserved.Should().BeTrue();
        store.ReplaceSawCallback.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_passes_display_safe_presentation_to_the_atomic_store_replacement()
    {
        FakeStore store = new();
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0)
        {
            History = "[]"
        });
        AgentConversationPresentation presentation = new("user turn", "display-safe answer", [Guid.CreateVersion7()]);

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with { Presentation = (_, _) => presentation }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        store.ReplacedPresentation.Should().Be(presentation);
    }

    private const string HistoryWithToolCall =
        "[{\"role\":\"user\",\"content\":\"find Sara\"},{\"type\":\"function_call\",\"callId\":\"c1\",\"name\":\"search_patients\",\"arguments\":\"{}\"},{\"type\":\"function_call_result\",\"callId\":\"c1\",\"name\":\"search_patients\",\"output\":\"[]\"},{\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":\"none\"}]}]";

    private const string CarriedHistory =
        "[{\"role\":\"user\",\"content\":\"find Sara\"},{\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":\"none\"}]}]";

    [Fact]
    public async Task Handle_continues_a_conversation_whose_configuration_changed_on_its_carried_over_messages()
    {
        FakeStore store = new() { History = HistoryWithToolCall, LeaseProfileChanged = true };
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        runner.Inputs.Single().History.Should().Be(CarriedHistory);
        store.ReplacedHistory.Should().Be("[]");
    }

    [Fact]
    public async Task Handle_compacts_a_history_too_large_to_grow_before_running()
    {
        string large = "[" + string.Join(',', Enumerable.Range(0, 8).Select(index =>
            $"{{\"role\":\"user\",\"content\":\"{index}{new string('x', 100 * 1024)}\"}}")) + "]";
        FakeStore store = new() { History = large };
        FakeRunner runner = new(new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        System.Text.Encoding.UTF8.GetByteCount(runner.Inputs.Single().History!)
            .Should().BeLessThanOrEqualTo(AgentConversationHistory.CompactionThresholdBytes);
    }

    [Fact]
    public async Task Handle_retries_once_on_carried_over_messages_when_the_runtime_rejects_the_history()
    {
        FakeStore store = new() { History = HistoryWithToolCall };
        FakeRunner runner = new(
            new AgentRunResult(false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(AgentRunErrorCode.InvalidProviderResponse, "invalid")),
            new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        runner.Inputs.Select(input => input.History).Should().Equal(HistoryWithToolCall, CarriedHistory);
    }

    [Fact]
    public async Task Handle_does_not_retry_a_rejected_history_after_a_tool_ran()
    {
        FakeStore store = new() { History = HistoryWithToolCall };
        FakeRunner runner = new(new AgentRunResult(
            false, null, null, new(0, 0), TimeSpan.Zero, 1, 1, new(AgentRunErrorCode.InvalidProviderResponse, "invalid")));

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.InvalidHistory);
        runner.Inputs.Should().ContainSingle("a tool may have changed data, so the turn must not run again");
    }

    [Fact]
    public async Task Handle_does_not_retry_a_rejected_history_that_was_already_carried_over()
    {
        FakeStore store = new() { History = HistoryWithToolCall, LeaseProfileChanged = true };
        FakeRunner runner = new(new AgentRunResult(
            false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(AgentRunErrorCode.InvalidProviderResponse, "invalid")));

        RunAgentConversationResult result =
            await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId), default);

        result.Outcome.Should().Be(AgentConversationOutcome.InvalidHistory);
        runner.Inputs.Should().ContainSingle();
        store.ReplacedHistory.Should().BeNull();
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

    [Fact]
    public async Task Handle_records_each_phase_in_order_with_call_counts_and_tokens_and_no_payload()
    {
        FakeStore store = new() { History = "[{\"role\":\"user\",\"content\":\"prior secret\"}]" };
        FakeRunner runner = new(new AgentRunResult(true, "answer text", null, new(120, 30), TimeSpan.Zero, 3, 2) { History = "[]" });
        CapturingLogger logger = new();

        await new RunAgentConversationHandler(store, runner, logger: logger)
            .Handle(Request(store.ConversationId) with { BeforeCommit = (_, _, _) => Task.FromResult(AgentConversationOutcome.Succeeded) }, default);

        logger.Messages.Where(message => message.Contains("phase") || message.Contains("runtime attempt"))
            .Select(message => message.Split(" ended")[0])
            .Should().Equal(
                "Agent turn phase LeaseAcquire",
                "Agent turn phase HistoryPreparation",
                "Agent runtime attempt 1",
                "Agent turn phase OutputValidation",
                "Agent turn phase Persistence",
                "Agent turn phase LeaseRelease");
        logger.Messages.Should().Contain(message =>
            message.Contains("model rounds 3, tool calls 2, input tokens 120, output tokens 30") &&
            message.Contains("execution execution-1"));
        logger.Messages.Last().Should().StartWith("Agent conversation workflow ended with Succeeded")
            .And.Contain("attempts 1");
        logger.Messages.Should().NotContain(message =>
            message.Contains("prior secret") || message.Contains("new input") || message.Contains("answer text"));
    }

    [Fact]
    public async Task Handle_records_a_busy_conversation_without_runtime_or_release_spans()
    {
        FakeStore store = new() { AcquireOutcome = AgentConversationOutcome.Busy };
        CapturingLogger logger = new();

        await new RunAgentConversationHandler(store, new FakeRunner(), logger: logger).Handle(Request(store.ConversationId), default);

        logger.Messages.Should().Contain(message => message.StartsWith("Agent turn phase LeaseAcquire ended with Busy"));
        logger.Messages.Should().NotContain(message => message.Contains("runtime attempt") || message.Contains("LeaseRelease"));
        logger.Messages.Last().Should().StartWith("Agent conversation workflow ended with Busy");
    }

    [Fact]
    public async Task Handle_records_both_attempts_when_a_rejected_history_is_retried()
    {
        FakeStore store = new() { History = HistoryWithToolCall };
        FakeRunner runner = new(
            new AgentRunResult(false, null, null, new(5, 0), TimeSpan.Zero, 1, 0, new(AgentRunErrorCode.InvalidProviderResponse, "invalid")),
            new AgentRunResult(true, "answer", null, new(7, 3), TimeSpan.Zero, 1, 0) { History = "[]" });
        CapturingLogger logger = new();

        await new RunAgentConversationHandler(store, runner, logger: logger).Handle(Request(store.ConversationId), default);

        logger.Messages.Should().Contain(message => message.StartsWith("Agent runtime attempt 1 ended with InvalidProviderResponse"));
        logger.Messages.Should().Contain(message => message.StartsWith("Agent runtime attempt 2 ended with Succeeded"));
        logger.Messages.Last().Should().Contain("attempts 2");
    }

    [Fact]
    public async Task Handle_records_cancellation_and_still_records_the_lease_release()
    {
        FakeStore store = new() { History = "[]" };
        CapturingLogger logger = new();
        ThrowingRunner runner = new();

        Func<Task> act = () => new RunAgentConversationHandler(store, runner, logger: logger).Handle(Request(store.ConversationId), default);

        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Messages.Should().Contain(message => message.StartsWith("Agent runtime attempt 1 ended with Cancelled"));
        logger.Messages.Should().Contain(message => message.StartsWith("Agent turn phase LeaseRelease ended with Succeeded"));
        logger.Messages.Last().Should().StartWith("Agent conversation workflow ended with Cancelled");
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Streamed_turn_is_accepted_once_after_the_lease_and_reports_stages_in_order()
    {
        FakeStore store = new() { History = "[]" };
        RecordingObserver observer = new();
        StreamedRunner runner = new(
            [new(AgentTurnStage.ModelRoundStarted, 1), new(AgentTurnStage.ToolStarted, 1), new(AgentTurnStage.ToolCompleted, 1)],
            new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 1) { History = "[]" });

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner).Handle(
            Request(store.ConversationId) with
            {
                Observer = observer,
                BeforeCommit = (_, _, _) => Task.FromResult(AgentConversationOutcome.Succeeded)
            }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        observer.Events.Should().Equal(
            "Accepted",
            "ModelRoundStarted:1",
            "ToolStarted:1",
            "ToolCompleted:1",
            "ValidatingResult:0",
            "SavingConversation:0");
        runner.StreamedCalls.Should().Be(1);
        runner.BufferedCalls.Should().Be(0);
        store.ReplacedHistory.Should().Be("[]");
    }

    [Fact]
    public async Task Streamed_turn_accepts_the_turn_with_a_deadline_beyond_the_runtime_timeout()
    {
        FakeStore store = new() { History = "[]" };
        RecordingObserver observer = new();
        StreamedRunner runner = new([], new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        await new RunAgentConversationHandler(store, runner).Handle(Request(store.ConversationId) with { Observer = observer }, default);

        observer.Deadline.Should().Be(Limits.EffectiveTimeout + RunAgentConversationHandler.TurnDeadlineMargin);
    }

    [Fact]
    public async Task Streamed_turn_that_is_not_accepted_never_reports_or_executes()
    {
        FakeStore store = new() { AcquireOutcome = AgentConversationOutcome.Busy };
        RecordingObserver observer = new();
        StreamedRunner runner = new([], new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0));

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with { Observer = observer }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.Busy);
        observer.Events.Should().BeEmpty();
        runner.StreamedCalls.Should().Be(0);
    }

    [Fact]
    public async Task Streamed_turn_fails_instead_of_retrying_a_rejected_history_after_answer_text_was_emitted()
    {
        FakeStore store = new() { History = HistoryWithToolCall };
        RecordingObserver observer = new() { AnswerTextEmitted = true };
        StreamedRunner runner = new(
            [],
            new AgentRunResult(false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(AgentRunErrorCode.InvalidProviderResponse, "invalid")),
            new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with { Observer = observer }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.InvalidHistory);
        runner.StreamedCalls.Should().Be(1, "a new attempt would silently replace the text the person already saw");
        store.ReplacedHistory.Should().BeNull();
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Streamed_turn_keeps_the_single_history_retry_before_any_answer_text()
    {
        FakeStore store = new() { History = HistoryWithToolCall };
        RecordingObserver observer = new();
        StreamedRunner runner = new(
            [],
            new AgentRunResult(false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(AgentRunErrorCode.InvalidProviderResponse, "invalid")),
            new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with { Observer = observer }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.Succeeded);
        runner.StreamedCalls.Should().Be(2);
        observer.Events.Count(e => e == "Accepted").Should().Be(1, "the retry stays under the same logical turn");
    }

    [Fact]
    public async Task Streamed_turn_passes_the_declared_answer_stream_to_the_runner_and_buffered_turns_never_do()
    {
        FakeStore store = new() { History = "[]" };
        AgentAnswerStream answer = new("message-1", ["answer"]);
        StreamedRunner runner = new([], new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        _ = await new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with { Observer = new RecordingObserver(), AnswerStream = answer }, default);

        runner.AnswerStreams.Should().ContainSingle().Which.Should().BeSameAs(answer);

        FakeStore bufferedStore = new() { History = "[]" };
        StreamedRunner bufferedRunner = new([], new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });
        _ = await new RunAgentConversationHandler(bufferedStore, bufferedRunner)
            .Handle(Request(bufferedStore.ConversationId) with { AnswerStream = answer }, default);
        bufferedRunner.StreamedCalls.Should().Be(0, "without an observer the turn is buffered and declares no answer stream");
    }

    [Fact]
    public async Task Streamed_turn_with_a_failed_validation_never_reports_saving_and_persists_nothing()
    {
        FakeStore store = new() { History = "[]" };
        RecordingObserver observer = new();
        StreamedRunner runner = new([], new AgentRunResult(true, "answer", null, new(1, 1), TimeSpan.Zero, 1, 0) { History = "[]" });

        RunAgentConversationResult result = await new RunAgentConversationHandler(store, runner).Handle(
            Request(store.ConversationId) with
            {
                Observer = observer,
                BeforeCommit = (_, _, _) => Task.FromResult(AgentConversationOutcome.InvalidHistory)
            }, default);

        result.Outcome.Should().Be(AgentConversationOutcome.InvalidHistory);
        observer.Events.Should().NotContain("SavingConversation:0");
        store.ReplacedHistory.Should().BeNull();
        store.Released.Should().BeTrue();
    }

    [Fact]
    public async Task Streamed_turn_releases_the_lease_when_it_is_cancelled()
    {
        FakeStore store = new() { History = "[]" };
        using CancellationTokenSource cancellation = new();
        StreamedRunner runner = new([], new AgentRunResult(true, "answer", null, new(0, 0), TimeSpan.Zero, 1, 0)) { OnRun = cancellation.Cancel };

        Func<Task> act = () => new RunAgentConversationHandler(store, runner)
            .Handle(Request(store.ConversationId) with { Observer = new RecordingObserver() }, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        store.Released.Should().BeTrue();
        store.ReplacedHistory.Should().BeNull();
    }

    private sealed class RecordingObserver : IAgentTurnObserver
    {
        public List<string> Events { get; } = [];
        public TimeSpan? Deadline { get; private set; }
        public bool AnswerTextEmitted { get; init; }

        public ValueTask AcceptedAsync(TimeSpan turnDeadline, CancellationToken cancellationToken)
        {
            Deadline = turnDeadline;
            Events.Add("Accepted");
            return ValueTask.CompletedTask;
        }

        public ValueTask StageAsync(AgentTurnStageEvent stage, CancellationToken cancellationToken)
        {
            Events.Add($"{stage.Stage}:{stage.Ordinal}");
            return ValueTask.CompletedTask;
        }

        public ValueTask AnswerDeltaAsync(string messageId, string text, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class StreamedRunner(IReadOnlyList<AgentTurnStageEvent> stages, params AgentRunResult[] results) : IAgentRunner
    {
        public int StreamedCalls { get; private set; }
        public List<AgentAnswerStream?> AnswerStreams { get; } = [];
        public int BufferedCalls { get; private set; }
        public Action? OnRun { get; init; }

        public Task<AgentRunResult> RunAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            CancellationToken cancellationToken = default)
        {
            BufferedCalls++;
            return Task.FromResult(results[0]);
        }

        public async Task<AgentRunResult> RunStreamedAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            AgentAnswerStream? answerStream,
            IAgentRunProgress progress,
            CancellationToken cancellationToken = default)
        {
            StreamedCalls++;
            AnswerStreams.Add(answerStream);
            OnRun?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            foreach (AgentTurnStageEvent stage in stages)
                await progress.StageAsync(stage, cancellationToken);
            return results[Math.Min(StreamedCalls - 1, results.Length - 1)];
        }
    }

    private sealed class CapturingLogger : ILogger<RunAgentConversationHandler>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private sealed class ThrowingRunner : IAgentRunner
    {
        public Task<AgentRunResult> RunAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            CancellationToken cancellationToken = default) => throw new OperationCanceledException();
    }

    private static RunAgentConversation Request(Guid? conversationId) => new(
        conversationId,
        Scope,
        Definition,
        new AgentInput("new input"),
        [],
        Context,
        Limits);

    private sealed class FakeRunner(params AgentRunResult[] results) : IAgentRunner
    {
        public AgentRunResult Result => results[0];
        public AgentInput? Input => Inputs.LastOrDefault();
        public List<AgentInput> Inputs { get; } = [];

        public Task<AgentRunResult> RunAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            CancellationToken cancellationToken = default)
        {
            Inputs.Add(input);
            return Task.FromResult(results[Math.Min(Inputs.Count - 1, results.Length - 1)]);
        }
    }

    private sealed class FakeStore : IAgentConversationStore
    {
        public Guid ConversationId { get; } = Guid.CreateVersion7();
        public string? History { get; init; }
        public bool LeaseProfileChanged { get; init; }
        public AgentConversationOutcome AcquireOutcome { get; init; } = AgentConversationOutcome.Succeeded;
        public AgentConversationOutcome ReplaceOutcome { get; init; } = AgentConversationOutcome.Succeeded;
        public AgentConversationOutcome DeleteOutcome { get; init; } = AgentConversationOutcome.Succeeded;
        public string? ReplacedHistory { get; private set; }
        public bool Released { get; private set; }
        public AgentConversationScope? DeletedScope { get; private set; }
        public bool CallbackObserved { get; set; }
        public bool ReplaceSawCallback { get; private set; }
        public AgentConversationPresentation? ReplacedPresentation { get; private set; }

        public Task<AgentConversationStoreResult> AcquireAsync(
            Guid? conversationId,
            AgentConversationScope scope,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) =>
            Task.FromResult(AcquireOutcome == AgentConversationOutcome.Succeeded
                ? new AgentConversationStoreResult(AcquireOutcome, new(ConversationId, Guid.CreateVersion7(), 2, History, ProfileChanged: LeaseProfileChanged))
                : new AgentConversationStoreResult(AcquireOutcome));

        public Task<AgentConversationOutcome> ReplaceHistoryAsync(
            AgentConversationLease lease,
            AgentConversationScope scope,
            string history,
            string historyFormat,
            CancellationToken cancellationToken)
        {
            ReplacedHistory = history;
            ReplaceSawCallback = CallbackObserved;
            return Task.FromResult(ReplaceOutcome);
        }

        public Task<AgentConversationOutcome> ReplaceHistoryAsync(
            AgentConversationLease lease,
            AgentConversationScope scope,
            string history,
            string historyFormat,
            AgentConversationPresentation? presentation,
            CancellationToken cancellationToken)
        {
            ReplacedHistory = history;
            ReplacedPresentation = presentation;
            ReplaceSawCallback = CallbackObserved;
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

        public Task<AgentConversationOwnerDeletionResult> DeleteForOwnerAsync(
            AgentConversationOwnerScope owner,
            CancellationToken cancellationToken) => Task.FromResult(new AgentConversationOwnerDeletionResult(AgentConversationOutcome.Succeeded, 0));

        public Task<IReadOnlyList<AgentConversationSummary>> ListAsync(
            AgentConversationScope scope,
            DateTimeOffset? before,
            int limit,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AgentConversationSummary>>([]);

        public Task<AgentConversationDisplay?> GetDisplayAsync(
            Guid conversationId,
            AgentConversationScope scope,
            CancellationToken cancellationToken) => Task.FromResult<AgentConversationDisplay?>(null);

        public Task<bool> RenameAsync(
            Guid conversationId,
            AgentConversationScope scope,
            string title,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<int> DeleteExpiredAsync(
            DateTimeOffset cutoff,
            CancellationToken cancellationToken) => Task.FromResult(0);
    }
}
