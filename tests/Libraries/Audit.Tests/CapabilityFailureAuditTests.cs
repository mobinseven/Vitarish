using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Audit.Tests;

public sealed class CapabilityFailureAuditTests
{
    private const string OperationId = "Files_Download";
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(StatusCodes.Status500InternalServerError, SecurityAuditReasonCatalog.EndpointException)]
    [InlineData(StatusCodes.Status502BadGateway, SecurityAuditReasonCatalog.EndpointException)]
    [InlineData(StatusCodes.Status503ServiceUnavailable, SecurityAuditReasonCatalog.DependencyUnavailable)]
    [InlineData(StatusCodes.Status504GatewayTimeout, SecurityAuditReasonCatalog.DependencyUnavailable)]
    public async Task A_failed_operation_records_its_endpoint_outcome_and_fixed_reason(int status, string reason)
    {
        RecordingWriter writer = new();

        await CreateRecorder(writer, userId: "user-1").RecordAsync(CreateContext(status));

        SecurityAuditEvent written = writer.Written.Should().ContainSingle().Subject;
        written.EventName.Should().Be(SecurityAuditEventCatalog.SecurityFailureEvent);
        written.EventCategory.Should().Be(SecurityAuditEventCatalog.SecurityFailureCategory);
        written.Outcome.Should().Be(SecurityAuditOutcome.Failed);
        written.Resource.Should().Be(SecurityAuditEventCatalog.EndpointResourcePrefix + OperationId);
        written.SourceBoundary.Should().Be(SecurityAuditEventCatalog.CapabilityFailureSourceBoundary);
        written.RedactedReason.Should().Be(reason);
        written.ActorId.Should().Be("user-1");
        written.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public async Task An_anonymous_failure_without_a_tenant_uses_no_identity_and_the_platform_scope()
    {
        RecordingWriter writer = new();

        await CreateRecorder(writer, userId: null, tenantId: null).RecordAsync(CreateContext(StatusCodes.Status500InternalServerError));

        SecurityAuditEvent written = writer.Written.Should().ContainSingle().Subject;
        written.ActorId.Should().BeNull();
        written.SubjectId.Should().BeNull();
        written.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
    }

    [Fact]
    public async Task An_endpoint_without_a_product_operation_is_not_recorded()
    {
        RecordingWriter writer = new();
        DefaultHttpContext context = new() { Response = { StatusCode = StatusCodes.Status503ServiceUnavailable } };
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "readiness"));

        await CreateRecorder(writer, "user-1").RecordAsync(context);

        writer.Written.Should().BeEmpty();
    }

    [Fact]
    public async Task Repeated_failures_in_one_window_share_a_key_and_a_later_window_does_not()
    {
        RecordingWriter writer = new();
        MutableTimeProvider time = new(Now);
        CapabilityFailureRecorder recorder = CreateRecorder(writer, "user-1", time: time);

        await recorder.RecordAsync(CreateContext(StatusCodes.Status503ServiceUnavailable));
        await recorder.RecordAsync(CreateContext(StatusCodes.Status503ServiceUnavailable));
        time.Advance(SecurityAuditOptions.DefaultDeniedReviewDedupeWindow);
        await recorder.RecordAsync(CreateContext(StatusCodes.Status503ServiceUnavailable));
        await recorder.RecordAsync(CreateContext(StatusCodes.Status500InternalServerError));

        string[] keys = writer.Written.Select(item => item.IdempotencyKey!).ToArray();
        keys[1].Should().Be(keys[0]);
        keys[2].Should().NotBe(keys[0]);
        keys[3].Should().NotBe(keys[2]);
    }

    [Fact]
    public async Task An_unavailable_audit_store_does_not_fail_the_completed_request()
    {
        ThrowingWriter writer = new(new SecurityAuditStoreException("store down", new InvalidOperationException()));

        Func<Task> record = () => CreateRecorder(writer, "user-1").RecordAsync(CreateContext(StatusCodes.Status503ServiceUnavailable));

        await record.Should().NotThrowAsync();
    }

    [Fact]
    public async Task An_unexpected_write_failure_is_logged_without_failing_the_completed_request()
    {
        RecordingLogger logger = new();

        await CreateRecorder(new ThrowingWriter(new InvalidOperationException("boom")), "user-1", logger: logger)
            .RecordAsync(CreateContext(StatusCodes.Status500InternalServerError));

        logger.Errors.Should().ContainSingle().Which.Should().Contain(OperationId).And.NotContain("boom");
    }

    [Fact]
    public async Task A_hanging_audit_store_is_bounded_by_the_write_timeout()
    {
        RecordingLogger logger = new();
        SecurityAuditOptions options = new() { DeniedReviewWriteTimeout = TimeSpan.FromMilliseconds(50) };

        await CreateRecorder(new HangingWriter(), "user-1", options: options, logger: logger)
            .RecordAsync(CreateContext(StatusCodes.Status500InternalServerError))
            .WaitAsync(TimeSpan.FromSeconds(30));

        logger.Errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK, 0)]
    [InlineData(StatusCodes.Status404NotFound, 0)]
    [InlineData(StatusCodes.Status499ClientClosedRequest, 0)]
    [InlineData(StatusCodes.Status503ServiceUnavailable, 1)]
    public async Task The_pipeline_records_only_server_errors(int status, int expected)
    {
        RecordingWriter writer = new();
        await using ServiceProvider provider = CreateServices(writer);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext context = new() { RequestServices = scope.ServiceProvider };
        context.SetEndpoint(Endpoint());

        await BuildPipeline(provider, inner =>
        {
            inner.Response.StatusCode = status;
            return Task.CompletedTask;
        })(context);

        writer.Written.Should().HaveCount(expected);
        context.Response.StatusCode.Should().Be(status);
    }

    [Fact]
    public async Task An_exception_escaping_the_handler_is_recorded_and_still_propagates()
    {
        RecordingWriter writer = new();
        await using ServiceProvider provider = CreateServices(writer);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext context = new() { RequestServices = scope.ServiceProvider };
        context.SetEndpoint(Endpoint());

        Func<Task> run = () => BuildPipeline(provider, _ => throw new InvalidOperationException("boom"))(context);

        await run.Should().ThrowAsync<InvalidOperationException>();
        writer.Written.Should().ContainSingle();
    }

    [Fact]
    public async Task A_cancelled_request_is_not_a_failed_capability()
    {
        RecordingWriter writer = new();
        await using ServiceProvider provider = CreateServices(writer);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DefaultHttpContext context = new() { RequestServices = scope.ServiceProvider };
        context.SetEndpoint(Endpoint());

        Func<Task> run = () => BuildPipeline(provider, _ => throw new OperationCanceledException())(context);

        await run.Should().ThrowAsync<OperationCanceledException>();
        writer.Written.Should().BeEmpty();
    }

    private static ServiceProvider CreateServices(ISecurityAuditWriter writer)
    {
        ServiceCollection services = new();
        services.AddSingleton(writer);
        services.AddScoped<IRequestContext>(_ => new TestRequestContext("tenant-1", "user-1"));
        services.AddSingleton(Options.Create(new SecurityAuditOptions()));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILogger<CapabilityFailureRecorder>>(NullLogger<CapabilityFailureRecorder>.Instance);
        services.AddTransient<CapabilityFailureRecorder>();
        return services.BuildServiceProvider();
    }

    private static RequestDelegate BuildPipeline(IServiceProvider provider, RequestDelegate endpoint)
    {
        ApplicationBuilder app = new(provider);
        app.UseCapabilityFailureAudit();
        app.Run(endpoint);
        return app.Build();
    }

    private static CapabilityFailureRecorder CreateRecorder(
        ISecurityAuditWriter writer,
        string? userId,
        string? tenantId = "tenant-1",
        SecurityAuditOptions? options = null,
        TimeProvider? time = null,
        ILogger<CapabilityFailureRecorder>? logger = null) =>
        new(
            writer,
            new TestRequestContext(tenantId, userId),
            Options.Create(options ?? new SecurityAuditOptions()),
            time ?? new MutableTimeProvider(Now),
            logger ?? NullLogger<CapabilityFailureRecorder>.Instance);

    private static DefaultHttpContext CreateContext(int status)
    {
        DefaultHttpContext context = new();
        context.Response.StatusCode = status;
        context.SetEndpoint(Endpoint());
        return context;
    }

    private static Endpoint Endpoint() =>
        new(null, new EndpointMetadataCollection(new ModuleEndpointOpenApiConvention(OperationId)), OperationId);

    private sealed class RecordingWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Written { get; } = [];

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Written.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class ThrowingWriter(Exception exception) : ISecurityAuditWriter
    {
        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default) =>
            Task.FromException<SecurityAuditWriteResult>(exception);
    }

    private sealed class HangingWriter : ISecurityAuditWriter
    {
        public async Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SecurityAuditWriteResult.Recorded;
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public void Advance(TimeSpan by) => current += by;

        public override DateTimeOffset GetUtcNow() => current;
    }

    private sealed class RecordingLogger : ILogger<CapabilityFailureRecorder>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                Errors.Add(formatter(state, exception));
        }
    }

    private sealed class TestRequestContext(string? tenantId, string? userId) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => userId;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "request-1";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity([], "test"));
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId };
    }
}
