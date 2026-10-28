using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Audit.Tests;

public sealed class AuditReviewDenialFilterTests
{
    private static readonly string[] ReviewOperations =
    [
        SecurityAuditApiOperations.ListSecurityEvents,
        SecurityAuditApiOperations.ListPlatformSecurityEvents,
        SecurityAuditApiOperations.GetPlatformSecurityEventCatalog,
        SecurityAuditApiOperations.ExportPlatformSecurityEvents
    ];

    [Theory]
    [MemberData(nameof(ReviewOperationData))]
    public async Task Each_review_endpoint_records_one_denial_naming_its_operation(string operationId)
    {
        RecordingWriter writer = new();
        using WebApplication app = CreateApp(writer, new DenyingQueryService(), new TestTimeProvider(DateTimeOffset.UtcNow));

        HttpContext context = await InvokeAsync(app, FindEndpoint(app, operationId));

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        SecurityAuditEvent denial = writer.Written.Should().ContainSingle().Subject;
        denial.EventName.Should().Be(SecurityAuditEventCatalog.AuditReviewEvent);
        denial.EventCategory.Should().Be(SecurityAuditEventCatalog.ReviewCategory);
        denial.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        denial.RedactedReason.Should().Be(SecurityAuditReasonCatalog.Unauthorized);
        denial.ActorId.Should().Be("user-7");
        denial.TenantId.Should().Be("tenant-a");
        denial.Operation.Should().Be(operationId);
        denial.Resource.Should().Be(SecurityAuditEventCatalog.EndpointResourcePrefix + operationId);
        SecurityAuditEventCatalog.IsCatalogued(denial).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ReviewOperationData))]
    public async Task Authorized_review_reads_write_nothing(string operationId)
    {
        RecordingWriter writer = new();
        using WebApplication app = CreateApp(writer, new AllowingQueryService(), new TestTimeProvider(DateTimeOffset.UtcNow));

        HttpContext context = await InvokeAsync(app, FindEndpoint(app, operationId));

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        writer.Written.Should().BeEmpty();
    }

    [Theory]
    [InlineData("store")]
    [InlineData("argument")]
    [InlineData("timeout")]
    public async Task Audit_write_failures_still_return_forbidden(string failure)
    {
        List<string> logs = [];
        ISecurityAuditWriter writer = failure switch
        {
            "store" => new ThrowingWriter(new SecurityAuditStoreException("Audit unavailable.", new InvalidOperationException())),
            "argument" => new ThrowingWriter(new ArgumentException("The denial event is invalid.", "auditEvent")),
            _ => new HangingWriter()
        };
        SecurityAuditOptions options = new()
        {
            DeniedReviewWriteTimeout = failure == "timeout" ? TimeSpan.FromMilliseconds(25) : TimeSpan.FromSeconds(2)
        };
        using WebApplication app = CreateApp(
            writer,
            new DenyingQueryService(),
            new TestTimeProvider(DateTimeOffset.UtcNow),
            options,
            logs);

        HttpContext context = await InvokeAsync(app, FindEndpoint(app, SecurityAuditApiOperations.ListPlatformSecurityEvents));

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        if (failure != "store")
            logs.Should().ContainSingle(message => message.Contains(AuditReviewDenialRecorder.WriteFailureCode));
    }

    [Theory]
    [MemberData(nameof(ReviewOperationData))]
    public async Task Authorization_middleware_records_anonymous_challenge_for_each_review_endpoint(string operationId)
    {
        RecordingWriter writer = new();
        using WebApplication app = CreateApp(writer, new AllowingQueryService(), new TestTimeProvider(DateTimeOffset.UtcNow), anonymous: true);

        HttpContext context = await InvokeAuthorizationAsync(app, FindEndpoint(app, operationId));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        SecurityAuditEvent denial = writer.Written.Should().ContainSingle().Subject;
        denial.ActorId.Should().BeNull();
        denial.SubjectId.Should().BeNull();
        denial.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
        denial.Operation.Should().Be(operationId);
        denial.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        SecurityAuditEventCatalog.IsCatalogued(denial).Should().BeTrue();
    }

    [Fact]
    public async Task Authorization_middleware_leaves_other_endpoint_challenges_unaudited()
    {
        const string unrelatedPath = "/unrelated";
        RecordingWriter writer = new();
        using WebApplication app = CreateApp(writer, new AllowingQueryService(), new TestTimeProvider(DateTimeOffset.UtcNow), anonymous: true);
        _ = app.MapGet(unrelatedPath, () => Results.Ok()).RequireAuthorization();
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().Single(item => item.RoutePattern.RawText == unrelatedPath);

        HttpContext context = await InvokeAuthorizationAsync(app, endpoint);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        writer.Written.Should().BeEmpty();
    }

    [Fact]
    public async Task Authorization_middleware_preserves_challenge_when_audit_store_fails()
    {
        ISecurityAuditWriter writer = new ThrowingWriter(new SecurityAuditStoreException("Audit unavailable.", new InvalidOperationException()));
        using WebApplication app = CreateApp(writer, new AllowingQueryService(), new TestTimeProvider(DateTimeOffset.UtcNow), anonymous: true);

        HttpContext context = await InvokeAuthorizationAsync(app, FindEndpoint(app, SecurityAuditApiOperations.ListSecurityEvents));

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Anonymous_challenges_are_persisted_once_within_the_dedupe_window()
    {
        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase("anonymous-denial-dedupe-" + Guid.NewGuid())
            .Options;
        await using AuditDbContext dbContext = new(options, new TestRequestContext(null, null));
        await dbContext.Database.EnsureCreatedAsync();
        TestTimeProvider timeProvider = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
        using WebApplication app = CreateApp(new SecurityAuditWriter(dbContext), new AllowingQueryService(), timeProvider, anonymous: true);
        RouteEndpoint endpoint = FindEndpoint(app, SecurityAuditApiOperations.ListPlatformSecurityEvents);

        _ = await InvokeAuthorizationAsync(app, endpoint);
        _ = await InvokeAuthorizationAsync(app, endpoint);

        SecurityAuditEvent denial = await dbContext.SecurityAuditEvents.SingleAsync();
        denial.ActorKind.Should().Be(SecurityAuditActorKind.Anonymous);
        denial.ActorId.Should().BeNull();
        denial.TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
        denial.Operation.Should().Be(SecurityAuditApiOperations.ListPlatformSecurityEvents);
    }

    [Fact]
    public async Task Repeating_a_denial_inside_the_window_writes_one_row()
    {
        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase("denial-dedupe-" + Guid.NewGuid())
            .Options;
        TestRequestContext requestContext = new("tenant-a", "user-7");
        await using AuditDbContext dbContext = new(options, requestContext);
        await dbContext.Database.EnsureCreatedAsync();
        TestTimeProvider timeProvider = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
        using WebApplication app = CreateApp(new SecurityAuditWriter(dbContext), new DenyingQueryService(), timeProvider);
        RouteEndpoint endpoint = FindEndpoint(app, SecurityAuditApiOperations.ListPlatformSecurityEvents);

        _ = await InvokeAsync(app, endpoint);
        _ = await InvokeAsync(app, endpoint);

        (await dbContext.SecurityAuditEvents.CountAsync()).Should().Be(1);
        SecurityAuditEvent recorded = await dbContext.SecurityAuditEvents.SingleAsync();
        recorded.Operation.Should().Be(SecurityAuditApiOperations.ListPlatformSecurityEvents);
        recorded.IdempotencyKey.Should().Contain(requestContext.UserId!).And.Contain(recorded.Operation);

        timeProvider.UtcNow = timeProvider.UtcNow.Add(SecurityAuditOptions.DefaultDeniedReviewDedupeWindow);
        _ = await InvokeAsync(app, endpoint);

        (await dbContext.SecurityAuditEvents.CountAsync()).Should().Be(2);
    }

    public static TheoryData<string> ReviewOperationData()
    {
        TheoryData<string> data = new();
        foreach (string operation in ReviewOperations)
            data.Add(operation);
        return data;
    }

    private static WebApplication CreateApp(
        ISecurityAuditWriter writer,
        ISecurityAuditQueryService queryService,
        TimeProvider timeProvider,
        SecurityAuditOptions? options = null,
        List<string>? logs = null,
        bool anonymous = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        if (logs is not null)
            builder.Logging.AddProvider(new ListLoggerProvider(logs));
        builder.Services.AddSingleton(writer);
        builder.Services.AddSingleton(queryService);
        builder.Services.AddSingleton<IRequestContext>(new TestRequestContext(anonymous ? null : "tenant-a", anonymous ? null : "user-7"));
        builder.Services.AddSingleton(Options.Create(options ?? new SecurityAuditOptions()));
        builder.Services.AddSingleton(timeProvider);
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditReviewAuthorizationResultHandler>();
        builder.Services.AddTransient<AuditReviewDenialRecorder>();
        builder.Services.AddSingleton<IAuthenticationService, StatusCodeAuthenticationService>();

        WebApplication app = builder.Build();
        _ = app.MapSecurityAuditApi();
        return app;
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string operationId) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

    private static async Task<HttpContext> InvokeAsync(WebApplication app, RouteEndpoint endpoint)
    {
        DefaultHttpContext context = new() { RequestServices = app.Services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = endpoint.RoutePattern.RawText ?? "/";
        context.Request.QueryString = new QueryString("?page=1&pageSize=10&sortBy=0&descending=true&uncataloguedOnly=false");
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(endpoint);

        await endpoint.RequestDelegate!(context);
        return context;
    }

    private static async Task<HttpContext> InvokeAuthorizationAsync(WebApplication app, RouteEndpoint endpoint)
    {
        DefaultHttpContext context = new() { RequestServices = app.Services };
        context.SetEndpoint(endpoint);
        IApplicationBuilder pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseAuthorization();
        pipeline.Run(_ => throw new InvalidOperationException("Authorization unexpectedly allowed the request."));
        await pipeline.Build()(context);
        return context;
    }

    // Turns Forbid and Challenge into their status codes, as the host's authentication handlers do.
    private sealed class StatusCodeAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();
    }

    private sealed class DenyingQueryService : ISecurityAuditQueryService
    {
        public Task<SecurityAuditPageReadDto> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken) =>
            throw new UnauthorizedAccessException();

        public Task<SecurityAuditPageReadDto> QueryPlatformAsync(SecurityAuditPlatformQuery query, CancellationToken cancellationToken) =>
            throw new UnauthorizedAccessException();

        public Task<IReadOnlyList<SecurityAuditCatalogEntryReadDto>> GetPlatformCatalogAsync(CancellationToken cancellationToken) =>
            throw new UnauthorizedAccessException();

        public Task<IReadOnlyList<SecurityAuditEventReadDto>> ExportPlatformAsync(SecurityAuditPlatformQuery query, CancellationToken cancellationToken) =>
            throw new UnauthorizedAccessException();

        public Task<IReadOnlyList<SecurityAuditEventReadDto>> GetByResourceAsync(string resource, CancellationToken cancellationToken) =>
            throw new UnauthorizedAccessException();

        public Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            throw new UnauthorizedAccessException();
    }

    private sealed class AllowingQueryService : ISecurityAuditQueryService
    {
        public Task<SecurityAuditPageReadDto> QueryAsync(SecurityAuditQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new SecurityAuditPageReadDto([], query.Page, query.PageSize, 0, false));

        public Task<SecurityAuditPageReadDto> QueryPlatformAsync(SecurityAuditPlatformQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new SecurityAuditPageReadDto([], query.Query.Page, query.Query.PageSize, 0, false));

        public Task<IReadOnlyList<SecurityAuditCatalogEntryReadDto>> GetPlatformCatalogAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SecurityAuditCatalogEntryReadDto>>([]);

        public Task<IReadOnlyList<SecurityAuditEventReadDto>> ExportPlatformAsync(SecurityAuditPlatformQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SecurityAuditEventReadDto>>([]);

        public Task<IReadOnlyList<SecurityAuditEventReadDto>> GetByResourceAsync(string resource, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SecurityAuditEventReadDto>>([]);

        public Task<SecurityAuditEventReadDto?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<SecurityAuditEventReadDto?>(null);
    }

    private sealed class RecordingWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Written { get; } = [];

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Written.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class ThrowingWriter(Exception exception) : ISecurityAuditWriter
    {
        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken) =>
            Task.FromException<SecurityAuditWriteResult>(exception);
    }

    private sealed class HangingWriter : ISecurityAuditWriter
    {
        public async Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return SecurityAuditWriteResult.Recorded;
        }
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
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

    private sealed class ListLoggerProvider(List<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(messages);

        public void Dispose() { }
    }

    private sealed class ListLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            messages.Add(formatter(state, exception));
    }
}
