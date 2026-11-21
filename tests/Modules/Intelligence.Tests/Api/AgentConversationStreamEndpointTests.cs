using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR;
using Moq;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Intelligence.Tests.Api;

public sealed class AgentConversationStreamEndpointTests
{
    private const string AgentKey = "sample.agent";
    private const string SubjectKind = "tenant";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(5);
    private static readonly RunAgentConversationRequest Request = new(AgentKey, SubjectKind, "subject-1", "hello");

    [Fact]
    public async Task Frames_arrive_in_order_with_monotonic_sequence_one_operation_and_a_single_terminal()
    {
        Fixture fixture = new((request, _) => RunTurnAsync(request, Succeeded()));

        IResult result = await fixture.StreamAsync();
        List<IntelligenceStreamChunk<AgentTurnPayload>> frames = fixture.Frames();

        result.Should().BeSameAs(Results.Empty);
        frames.Select(frame => frame.Kind).Should().Equal(
            StreamChunkKind.Started, StreamChunkKind.Progress, StreamChunkKind.Progress, StreamChunkKind.Progress, StreamChunkKind.Completed);
        frames.Select(frame => frame.Metadata.Sequence).Should().Equal(0, 1, 2, 3, 4);
        frames.Select(frame => frame.Metadata.OperationId).Distinct().Should().ContainSingle();
        frames.Count(frame => frame.Kind is StreamChunkKind.Completed or StreamChunkKind.Failed or StreamChunkKind.Canceled).Should().Be(1);
        frames[1].Delta!.Stage.Should().Be(AgentTurnProgressStage.ModelRoundStarted);
        frames[1].Delta!.Ordinal.Should().Be(1);
        frames[4].Delta!.Result!.AgentRunId.Should().Be("run-1");
        fixture.Context.Response.ContentType.Should().Be(ModuleEndpointOpenApiConventions.JsonStream);
        fixture.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Disabled_streaming_is_a_clear_error_before_headers_and_before_execution()
    {
        Fixture fixture = new((request, _) => RunTurnAsync(request, Succeeded()), streamingEnabled: false);

        IResult result = await fixture.StreamAsync();
        await result.ExecuteAsync(fixture.Context);

        fixture.Context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        fixture.Calls.Should().Be(0);
        IntelligenceError error = JsonSerializer.Deserialize<IntelligenceError>(fixture.Body(), Web)!;
        error.Code.Should().Be(IntelligenceErrorCode.StreamingNotEnabled);
        fixture.Context.Response.ContentType.Should().NotBe(ModuleEndpointOpenApiConventions.JsonStream);
    }

    [Fact]
    public async Task Invalid_request_and_unknown_route_are_standard_errors_without_execution()
    {
        Fixture fixture = new((request, _) => RunTurnAsync(request, Succeeded()));

        IResult invalid = await AgentConversationStreamEndpoint.StreamAsync(
            Request with { Message = " " }, fixture.Registry, NullLogger.Instance, TimeProvider.System, fixture.Context, WriteTimeout, default);
        IResult unknown = await AgentConversationStreamEndpoint.StreamAsync(
            Request with { AgentKey = "missing" }, fixture.Registry, NullLogger.Instance, TimeProvider.System, fixture.Context, WriteTimeout, default);

        await invalid.ExecuteAsync(fixture.Context);
        fixture.Context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        fixture.Context.Response.Body.SetLength(0);
        await unknown.ExecuteAsync(fixture.Context);
        fixture.Context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        fixture.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(AgentConversationOutcome.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(AgentConversationOutcome.Busy, StatusCodes.Status409Conflict)]
    public async Task Denial_before_acceptance_is_a_standard_http_error_without_a_stream(AgentConversationOutcome outcome, int status)
    {
        // The app endpoint authorizes the practice and subject before the workflow accepts the turn.
        Fixture fixture = new((_, _) => Task.FromResult(new AgentConversationEndpointResult(outcome)));

        IResult result = await fixture.StreamAsync();
        await result.ExecuteAsync(fixture.Context);

        fixture.Context.Response.StatusCode.Should().Be(status);
        fixture.Context.Response.ContentType.Should().NotBe(ModuleEndpointOpenApiConventions.JsonStream);
        fixture.Body().Should().NotContain("\"kind\"");
    }

    [Fact]
    public async Task Failed_validation_or_persistence_after_acceptance_is_a_failed_frame_never_completed()
    {
        Fixture fixture = new(async (request, ct) =>
        {
            await request.Observer!.AcceptedAsync(TimeSpan.FromMinutes(1), ct);
            await request.Observer.StageAsync(new(AgentTurnStage.SavingConversation), ct);
            return new AgentConversationEndpointResult(AgentConversationOutcome.InvalidHistory);
        });

        _ = await fixture.StreamAsync();
        List<IntelligenceStreamChunk<AgentTurnPayload>> frames = fixture.Frames();

        frames.Select(frame => frame.Kind).Should().Equal(StreamChunkKind.Started, StreamChunkKind.Progress, StreamChunkKind.Failed);
        frames.Should().NotContain(frame => frame.Kind == StreamChunkKind.Completed);
        frames[^1].Error!.Code.Should().Be(IntelligenceErrorCode.InvalidHistory);
        frames[^1].Error!.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Failure_after_acceptance_is_redacted_and_leaks_no_exception_text()
    {
        Fixture fixture = new(async (request, ct) =>
        {
            await request.Observer!.AcceptedAsync(TimeSpan.FromMinutes(1), ct);
            throw new InvalidOperationException("secret-token-123 prompt text");
        });

        _ = await fixture.StreamAsync();

        string body = fixture.Body();
        body.Should().NotContain("secret-token-123").And.NotContain("prompt text");
        fixture.Frames()[^1].Kind.Should().Be(StreamChunkKind.Failed);
        fixture.Frames()[^1].Error!.Code.Should().Be(IntelligenceErrorCode.UnexpectedFailure);
    }

    [Fact]
    public async Task Server_deadline_on_an_intact_connection_is_a_canceled_terminal_frame()
    {
        Fixture fixture = new(async (request, ct) =>
        {
            await request.Observer!.AcceptedAsync(TimeSpan.FromMilliseconds(30), ct);
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new AgentConversationEndpointResult(AgentConversationOutcome.Succeeded);
        });

        _ = await fixture.StreamAsync();

        fixture.Frames().Select(frame => frame.Kind).Should().Equal(StreamChunkKind.Started, StreamChunkKind.Canceled);
    }

    [Fact]
    public async Task Client_abort_cancels_the_turn_and_writes_no_terminal_frame()
    {
        using CancellationTokenSource abort = new();
        bool observedCancellation = false;
        Fixture fixture = new(async (request, ct) =>
        {
            await request.Observer!.AcceptedAsync(TimeSpan.FromMinutes(1), ct);
            abort.Cancel();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException)
            {
                observedCancellation = true;
                throw;
            }
            return new AgentConversationEndpointResult(AgentConversationOutcome.Succeeded);
        });
        fixture.Context.RequestAborted = abort.Token;

        Func<Task> act = () => fixture.StreamAsync(abort.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        observedCancellation.Should().BeTrue();
        fixture.Frames().Select(frame => frame.Kind).Should().Equal(StreamChunkKind.Started);
    }

    [Fact]
    public async Task Slow_consumer_is_bounded_by_the_write_timeout_and_the_turn_is_cancelled()
    {
        BlockingBody body = new();
        bool observedCancellation = false;
        Fixture fixture = new(async (request, ct) =>
        {
            await request.Observer!.AcceptedAsync(TimeSpan.FromMinutes(1), ct);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException)
            {
                observedCancellation = true;
                throw;
            }
            return new AgentConversationEndpointResult(AgentConversationOutcome.Succeeded);
        }, body: body);

        IResult result = await fixture.StreamAsync(writeTimeout: TimeSpan.FromMilliseconds(50));

        result.Should().BeSameAs(Results.Empty);
        observedCancellation.Should().BeTrue("a consumer that stops reading must not hold the turn or its lease open");
        body.Writes.Should().Be(1, "no frame is written after the consumer failed");
    }

    [Fact]
    public async Task Disconnected_consumer_cancels_the_turn_without_writing_more()
    {
        BlockingBody body = new() { ThrowIo = true };
        bool observedCancellation = false;
        Fixture fixture = new(async (request, ct) =>
        {
            await request.Observer!.AcceptedAsync(TimeSpan.FromMinutes(1), ct);
            await request.Observer.StageAsync(new(AgentTurnStage.ModelRoundStarted, 1), ct);
            observedCancellation = ct.IsCancellationRequested;
            return new AgentConversationEndpointResult(AgentConversationOutcome.Succeeded);
        }, body: body);

        _ = await fixture.StreamAsync();

        observedCancellation.Should().BeTrue();
        body.Writes.Should().Be(1);
    }

    [Fact]
    public void Stream_and_buffered_routes_share_the_same_authorization_tenant_and_antiforgery_protection()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        // Endpoint registration infers each handler parameter source, so every injected service must resolve.
        builder.Services.AddSingleton(Mock.Of<IResponder>());
        builder.Services.AddSingleton(Mock.Of<ISender>());
        builder.Services.AddSingleton(Mock.Of<IRequestContext>());
        builder.Services.AddSingleton(new LiveTranscriptionSettings());
        builder.Services.AddSingleton(new RealtimeTranscriptionOriginPolicy([]));
        builder.Services.AddSingleton(new LiveTranscriptionSessionFactory(Mock.Of<ITranscriptionProvider>(), new LiveTranscriptionSettings(), TimeProvider.System));
        builder.Services.AddSingleton(new LiveTranscriptionCapabilitiesQuery(Mock.Of<ITranscriptionProvider>(), new LiveTranscriptionSettings()));
        using WebApplication app = builder.Build();
        _ = app.MapIntelligenceApi();
        List<Endpoint> endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToList();
        Endpoint Find(string name) => endpoints.Single(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);

        Endpoint buffered = Find("IntelligenceRunAgentConversation");
        Endpoint stream = Find(IntelligenceApiOperations.StreamAgentConversation);
        Endpoint capabilities = Find(IntelligenceApiOperations.GetAgentConversationCapabilities);

        foreach (Endpoint endpoint in new[] { buffered, stream, capabilities })
        {
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().Contain(data => data.Policy == IdentityDefinitions.TenantAccess);
            endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().NotBeNull();
        }
        buffered.Metadata.GetMetadata<ModuleEndpointAntiforgeryMetadata>().Should().NotBeNull();
        stream.Metadata.GetMetadata<ModuleEndpointAntiforgeryMetadata>().Should().NotBeNull();
        stream.Metadata.GetMetadata<StreamingEndpointMetadata>()!.ContentType.Should().Be(ModuleEndpointOpenApiConventions.JsonStream);
        stream.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should().EndWith(IntelligenceApiRoutes.AgentConversationStream);
        stream.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
    }

    private static async Task<AgentConversationEndpointResult> RunTurnAsync(AgentConversationEndpointRequest request, AgentConversationEndpointResult result)
    {
        IAgentTurnObserver observer = request.Observer!;
        await observer.AcceptedAsync(TimeSpan.FromMinutes(1), default);
        await observer.StageAsync(new(AgentTurnStage.ModelRoundStarted, 1), default);
        await observer.StageAsync(new(AgentTurnStage.ValidatingResult), default);
        await observer.StageAsync(new(AgentTurnStage.SavingConversation), default);
        return result;
    }

    private static AgentConversationEndpointResult Succeeded() => new(
        AgentConversationOutcome.Succeeded, "run-1", Guid.Parse("11111111-1111-1111-1111-111111111111"),
        JsonDocument.Parse("{\"messages\":[]}").RootElement.Clone());

    private sealed class Fixture
    {
        private readonly MemoryStream memory = new();

        public Fixture(
            Func<AgentConversationEndpointRequest, CancellationToken, Task<AgentConversationEndpointResult>> run,
            bool streamingEnabled = true,
            Stream? body = null)
        {
            Endpoint = new(run, streamingEnabled, () => Calls++);
            Registry = new AgentConversationEndpointRegistry([Endpoint]);
            Context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
            Context.Response.Body = body ?? memory;
        }

        public int Calls { get; private set; }
        public StubEndpoint Endpoint { get; }
        public IAgentConversationEndpointRegistry Registry { get; }
        public DefaultHttpContext Context { get; }

        public Task<IResult> StreamAsync(CancellationToken ct = default, TimeSpan? writeTimeout = null) =>
            AgentConversationStreamEndpoint.StreamAsync(
                Request, Registry, NullLogger.Instance, TimeProvider.System, Context, writeTimeout ?? WriteTimeout, ct);

        public string Body() => Encoding.UTF8.GetString(memory.ToArray());

        public List<IntelligenceStreamChunk<AgentTurnPayload>> Frames() => Body()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<IntelligenceStreamChunk<AgentTurnPayload>>(line, Web)!)
            .ToList();
    }

    private sealed class StubEndpoint(
        Func<AgentConversationEndpointRequest, CancellationToken, Task<AgentConversationEndpointResult>> run,
        bool streamingEnabled,
        Action onRun) : IAgentConversationEndpoint
    {
        public IReadOnlyCollection<AgentConversationRoute> Routes { get; } = [new(AgentKey, SubjectKind, streamingEnabled)];

        public Task<AgentConversationEndpointResult> RunAsync(AgentConversationEndpointRequest request, CancellationToken cancellationToken)
        {
            onRun();
            return run(request, cancellationToken);
        }
    }

    private sealed class BlockingBody : Stream
    {
        public int Writes { get; private set; }
        public bool ThrowIo { get; init; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Writes++;
            if (ThrowIo)
                throw new IOException("The client disconnected.");
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
