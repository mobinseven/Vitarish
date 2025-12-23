using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Interface.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Contracts;
using Xunit;

namespace Vitarish.IntegrationTests.Authorization;

public sealed class AuthorizationTests
{
    [Fact]
    public async Task MinimalApi_returns_200_for_admin_in_tenant()
    {
        using AuthorizationWebAppFactory factory = new();
        factory.RoleStore.SetRoles("t1", "u1", "Admin");

        using HttpClient client = factory.CreateAuthenticatedClient("u1", "t1");

        HttpResponseMessage response = await client.GetAsync("/api/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MinimalApi_returns_403_for_missing_admin_role()
    {
        using AuthorizationWebAppFactory factory = new();
        factory.RoleStore.SetRoles("t1", "u1", "Editor");

        using HttpClient client = factory.CreateAuthenticatedClient("u1", "t1");

        HttpResponseMessage response = await client.GetAsync("/api/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MinimalApi_returns_401_when_unauthenticated()
    {
        using AuthorizationWebAppFactory factory = new();
        factory.RoleStore.SetRoles("t1", "u1", "Admin");

        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "t1";

        HttpResponseMessage response = await client.GetAsync("/api/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MinimalApi_respects_tenant_switch()
    {
        using AuthorizationWebAppFactory factory = new();
        factory.RoleStore.SetRoles("t1", "u1", "Admin");

        using HttpClient client = factory.CreateAuthenticatedClient("u1", "t1");
        HttpResponseMessage t1Response = await client.GetAsync("/api/admin");

        using HttpClient clientT2 = factory.CreateAuthenticatedClient("u1", "t2");
        HttpResponseMessage t2Response = await clientT2.GetAsync("/api/admin");

        t1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        t2Response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Blazor_route_renders_admin_content_when_authorized()
    {
        using AuthorizationWebAppFactory factory = new();
        factory.RoleStore.SetRoles("t1", "u1", "Admin");

        using HttpClient client = factory.CreateAuthenticatedClient("u1", "t1");

        string html = await client.GetStringAsync("/admin");

        html.Should().Contain("id=\"admin-content\"");
        html.Should().NotContain("id=\"not-authorized\"");
    }

    [Fact]
    public async Task Blazor_route_renders_not_authorized_when_role_missing()
    {
        using AuthorizationWebAppFactory factory = new();
        factory.RoleStore.SetRoles("t1", "u1", "Editor");

        using HttpClient client = factory.CreateAuthenticatedClient("u1", "t1");

        string html = await client.GetStringAsync("/admin");

        html.Should().Contain("id=\"not-authorized\"");
        html.Should().NotContain("id=\"admin-content\"");
    }
}

internal sealed class AuthorizationWebAppFactory : WebApplicationFactory<Program>
{
    private static readonly Dictionary<string, string?> _requiredSettings = new()
    {
        ["Vitarish:Identity:Authority"] = "https://example.com",
        ["Vitarish:Identity:ClientId"] = "client-id",
        ["Vitarish:Identity:Secret"] = "secret",
        ["Vitarish:Identity:DataProtectionPassword"] = "test-password",
        ["Vitarish:Identity:DisableDataProtection"] = "true",
        ["ConnectionStrings:Redis"] = "localhost:6379",
        ["ConnectionStrings:Db"] = "Server=(localdb)\\mssqllocaldb;Database=VitarishTests;Trusted_Connection=True;MultipleActiveResultSets=true"
    };

    static AuthorizationWebAppFactory()
    {
        foreach ((string key, string? value) in _requiredSettings)
        {
            string envKey = key.Replace(":", "__", StringComparison.Ordinal);
            Environment.SetEnvironmentVariable(envKey, value);
        }
    }

    public TestTenantRoleStore RoleStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach ((string key, string? value) in _requiredSettings)
        {
            builder.UseSetting(key, value);
        }
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(_requiredSettings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IUsersService));
            services.AddSingleton(RoleStore);
            services.AddScoped<IUsersService, TestUsersService>();
            services.RemoveAll<IClaimsTransformation>();
            services.AddSingleton<IClaimsTransformation, TestTenantRoleClaimsTransformation>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string userId, string tenant)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"https://{tenant}")
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, userId);
        client.DefaultRequestHeaders.Host = tenant;
        return client;
    }
}

internal sealed class TestTenantRoleStore
{
    private readonly Dictionary<(string Tenant, string UserId), string[]> _roles = new();

    public void SetRoles(string tenant, string userId, params string[] roles) =>
        _roles[(tenant, userId)] = roles;

    public string[] GetRoles(string tenant, string userId) =>
        _roles.TryGetValue((tenant, userId), out string[]? value) ? value : [];

    public bool HasRole(string tenant, string userId, string role) =>
        GetRoles(tenant, userId).Contains(role, StringComparer.Ordinal);
}

internal sealed class TestTenantRoleClaimsTransformation(
    IHttpContextAccessor accessor,
    TestTenantRoleStore store) : IClaimsTransformation
{
    private const string AuthType = "TenantRoles";
    private const string TenantIdClaim = "tenant_id";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        HttpContext? context = accessor.HttpContext;
        if (context is null)
        {
            return Task.FromResult(principal);
        }

        string tenant = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(tenant))
        {
            return Task.FromResult(principal);
        }

        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(principal);
        }

        string[] roles = store.GetRoles(tenant, userId);
        IEnumerable<ClaimsIdentity> filteredIdentities = principal.Identities
            .Where(i => i.AuthenticationType != AuthType);

        if (roles.Length == 0)
        {
            return Task.FromResult(new ClaimsPrincipal(filteredIdentities));
        }

        ClaimsIdentity tenantIdentity = new(authenticationType: AuthType);
        tenantIdentity.AddClaim(new Claim(TenantIdClaim, tenant));
        foreach (string role in roles)
        {
            tenantIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        ClaimsPrincipal rebuilt = new(filteredIdentities.Append(tenantIdentity));
        return Task.FromResult<ClaimsPrincipal>(rebuilt);
    }
}

internal sealed class TestUsersService(TestTenantRoleStore store, IHttpContextAccessor accessor) : IUsersService
{
    public IEnumerable<UserDto> Users => [];
    public UserDto? User => null;

    public Task<bool> CheckUserRole(string roleName, CancellationToken cancellationToken = default)
    {
        HttpContext? context = accessor.HttpContext;
        string tenant = context?.Request.Host.Host ?? string.Empty;
        string? userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(store.HasRole(tenant, userId, roleName));
    }

    public Task<UserDto?> GetCurrentUser(CancellationToken cancellationToken = default) =>
        Task.FromResult<UserDto?>(null);

    public Task<IEnumerable<UserDto>> GetUsersByRole(string roleName, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<UserDto>>(Array.Empty<UserDto>());

    public Task Initialize(string[] roles) => Task.CompletedTask;
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith(SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing test auth header"));
        }

        string? userId = header[SchemeName.Length..].Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing user id"));
        }

        ClaimsIdentity identity = new(authenticationType: SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
