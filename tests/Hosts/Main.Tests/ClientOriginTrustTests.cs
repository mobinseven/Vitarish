using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;

public sealed class ClientOriginTrustTests
{
    private const string ExactOrigin = "https://app.example.test";
    private const string TrustedTenantHost = "rezaei.clinic.example.test";
    private const string TrustedTenantOrigin = "https://rezaei.clinic.example.test";

    [Fact]
    public async Task Exact_allowed_origin_is_trusted_without_calling_tenant_origin_trust()
    {
        Mock<ITenantOriginTrust> tenantTrust = CreateTenantTrustMock();
        using WebApplication app = BuildApp(tenantTrust.Object);

        (string? clientHost, bool corsAllowed) = await InvokePipelineAsync(app, ExactOrigin);

        Assert.Equal("app.example.test", clientHost);
        Assert.True(corsAllowed);
        tenantTrust.Verify(t => t.IsTenantOriginAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Active_tenant_origin_is_trusted_for_client_host_and_cors()
    {
        Mock<ITenantOriginTrust> tenantTrust = CreateTenantTrustMock();
        using WebApplication app = BuildApp(tenantTrust.Object);

        (string? clientHost, bool corsAllowed) = await InvokePipelineAsync(app, TrustedTenantOrigin);

        Assert.Equal(TrustedTenantHost, clientHost);
        Assert.True(corsAllowed);
        tenantTrust.Verify(t => t.IsTenantOriginAsync(It.Is<Uri>(u => u.Host == TrustedTenantHost), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Untrusted_tenant_origin_is_rejected_for_client_host_and_cors()
    {
        Mock<ITenantOriginTrust> tenantTrust = CreateTenantTrustMock();
        using WebApplication app = BuildApp(tenantTrust.Object);

        (string? clientHost, bool corsAllowed) = await InvokePipelineAsync(app, "https://evil.clinic.example.test");

        Assert.Null(clientHost);
        Assert.False(corsAllowed);
    }

    [Theory]
    [InlineData("https://rezaei.clinic.example.test/path")]
    [InlineData("https://a.test, https://b.test")]
    public async Task Malformed_or_multiple_origin_headers_do_not_call_tenant_origin_trust(string originHeader)
    {
        Mock<ITenantOriginTrust> tenantTrust = CreateTenantTrustMock();
        using WebApplication app = BuildApp(tenantTrust.Object);

        (string? clientHost, bool corsAllowed) = await InvokePipelineAsync(app, originHeader);

        Assert.Null(clientHost);
        Assert.False(corsAllowed);
        tenantTrust.Verify(t => t.IsTenantOriginAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Without_tenant_origin_trust_registered_exact_origin_works_and_tenant_origin_is_rejected()
    {
        using WebApplication app = BuildApp(tenantOriginTrust: null);

        (string? exactClientHost, bool exactCorsAllowed) = await InvokePipelineAsync(app, ExactOrigin);
        (string? tenantClientHost, bool tenantCorsAllowed) = await InvokePipelineAsync(app, TrustedTenantOrigin);

        Assert.Equal("app.example.test", exactClientHost);
        Assert.True(exactCorsAllowed);
        Assert.Null(tenantClientHost);
        Assert.False(tenantCorsAllowed);
    }

    [Fact]
    public async Task Unavailable_tenant_origin_trust_fails_closed_without_failing_the_request()
    {
        Mock<ITenantOriginTrust> tenantTrust = new();
        _ = tenantTrust
            .Setup(t => t.IsTenantOriginAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("store unavailable"));
        using WebApplication app = BuildApp(tenantTrust.Object);

        (string? clientHost, bool corsAllowed) = await InvokePipelineAsync(app, TrustedTenantOrigin);
        int webSocketStatus = await InvokeWebSocketUpgradeAsync(app, TrustedTenantOrigin);

        Assert.Null(clientHost);
        Assert.False(corsAllowed);
        Assert.Equal(StatusCodes.Status403Forbidden, webSocketStatus);
    }

    [Theory]
    [InlineData(ExactOrigin, StatusCodes.Status200OK)]
    [InlineData(TrustedTenantOrigin, StatusCodes.Status200OK)]
    [InlineData("https://evil.clinic.example.test", StatusCodes.Status403Forbidden)]
    [InlineData("https://rezaei.clinic.example.test/path", StatusCodes.Status403Forbidden)]
    public async Task WebSocket_upgrade_requires_a_trusted_origin(string origin, int expectedStatus)
    {
        using WebApplication app = BuildApp(CreateTenantTrustMock().Object);

        int status = await InvokeWebSocketUpgradeAsync(app, origin);

        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task WebSocket_upgrade_without_an_origin_header_is_left_to_the_endpoint()
    {
        using WebApplication app = BuildApp(CreateTenantTrustMock().Object);

        int status = await InvokeWebSocketUpgradeAsync(app, origin: null);

        Assert.Equal(StatusCodes.Status200OK, status);
    }

    private static async Task<int> InvokeWebSocketUpgradeAsync(WebApplication app, string? origin)
    {
        using IServiceScope scope = app.Services.CreateScope();
        DefaultHttpContext context = new() { RequestServices = scope.ServiceProvider };
        context.Features.Set<IHttpWebSocketFeature>(new UpgradeRequestFeature());
        if (origin is not null)
            context.Request.Headers.Origin = origin;

        ApplicationBuilder appBuilder = new(app.Services);
        _ = appBuilder.UseClientOriginTrust();
        _ = appBuilder.UseTrustedWebSocketOrigins();
        appBuilder.Run(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        await appBuilder.Build()(context);
        return context.Response.StatusCode;
    }

    private sealed class UpgradeRequestFeature : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => true;

        public Task<System.Net.WebSockets.WebSocket> AcceptAsync(WebSocketAcceptContext context) => throw new NotSupportedException();
    }

    private static Mock<ITenantOriginTrust> CreateTenantTrustMock()
    {
        Mock<ITenantOriginTrust> mock = new();
        _ = mock
            .Setup(t => t.IsTenantOriginAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Uri uri, CancellationToken _) => uri.Host == TrustedTenantHost);
        return mock;
    }

    private static WebApplication BuildApp(ITenantOriginTrust? tenantOriginTrust)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.Services.AddHttpContextAccessor();
        builder.AddHostCors([ExactOrigin]);
        if (tenantOriginTrust is not null)
            _ = builder.Services.AddSingleton(tenantOriginTrust);
        return builder.Build();
    }

    private static async Task<(string? ClientHost, bool CorsAllowed)> InvokePipelineAsync(WebApplication app, StringValues originHeader)
    {
        using IServiceScope scope = app.Services.CreateScope();
        DefaultHttpContext context = new() { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Origin = originHeader;

        IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = context;

        string? clientHost = null;
        bool corsAllowed = false;

        ApplicationBuilder appBuilder = new(app.Services);
        _ = appBuilder.UseClientOriginTrust();
        appBuilder.Run(async ctx =>
        {
            clientHost = ctx.RequestServices.GetRequiredService<IClientInterfaceHostAccessor>().ClientInterfaceHost;
            ICorsPolicyProvider policyProvider = ctx.RequestServices.GetRequiredService<ICorsPolicyProvider>();
            CorsPolicy? policy = await policyProvider.GetPolicyAsync(ctx, "WebHostCors");
            corsAllowed = policy is not null && policy.IsOriginAllowed(originHeader.ToString());
        });

        RequestDelegate pipeline = appBuilder.Build();
        await pipeline(context);

        return (clientHost, corsAllowed);
    }
}
