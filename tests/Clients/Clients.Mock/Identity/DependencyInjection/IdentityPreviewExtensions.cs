using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Interface.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Contracts;
using Vitarish.Clients.Interface.Identity.Extensions;
using Vitarish.Clients.Interface.Identity.Services;

namespace Vitarish.Clients.Mock.Identity.DependencyInjection;

public static class IdentityPreviewExtensions
{
    private const string HttpClientName = "Vitarish.Clients.Mock";

    public static IServiceCollection AddIdentityComponentsPreview(this IServiceCollection services)
    {
        if (services.Any(sd => sd.ServiceType == typeof(IUsersService)))
        {
            return services;
        }

        _ = services.Configure<IdentityClientOptions>(options =>
        {
            options.Authority = "https://localhost";
            options.ClientId = "preview-client";
            options.Secret = "preview-secret";
            options.Local = true;
            options.Dev = true;
            options.CookieExpirationDays = 1;
        });

        if (!services.Any(sd => sd.ServiceType == typeof(IDistributedCache)))
        {
            _ = services.AddDistributedMemoryCache();
        }

        _ = services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(MockAuthenticationDefaults.Scheme, _ => { });

        _ = services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme ??= MockAuthenticationDefaults.Scheme;
            options.DefaultAuthenticateScheme ??= MockAuthenticationDefaults.Scheme;
            options.DefaultChallengeScheme ??= MockAuthenticationDefaults.Scheme;
        });

        _ = services.AddMemoryCache();
        _ = services.AddAuthorization();
        _ = services.AddCascadingAuthenticationState();
        _ = services.AddHttpContextAccessor();
        _ = services.AddHttpClient();
        _ = services.AddTransient<MockHttpMessageHandler>();
        _ = services.AddHttpClient(HttpClientName)
            .ConfigurePrimaryHttpMessageHandler<MockHttpMessageHandler>();
        _ = services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
        _ = services.AddSingleton<IAuthorizationPolicyProvider, TenantRolePolicyProvider>();
        _ = services.AddScoped<IAuthorizationHandler, TenantRoleHandler>();
        _ = services.AddScoped<IIdentityService, MockIdentityService>();
        _ = services.AddScoped<IUsersService, MockUsersService>();
        _ = services.AddScoped<IPersonalAccessTokenClient, MockPersonalAccessTokenClient>();
        _ = services.AddScoped<IPersonalClientFactory, MockPersonalClientFactory>();
        _ = services.AddTransient<IIdentityClientService, MockIdentityClientService>();
        _ = services.AddScoped<UserProfileClient>();
        _ = services.AddScoped<UserProfileCultureInfoProvider>();

        return services;
    }

    private static class MockAuthenticationDefaults
    {
        public const string Scheme = "preview";
        private const string UserName = "preview-user";
        private const string Subject = "preview-user";
        private const string AdminRole = "admin.local:::Admin";
        private const string AccessClaimType = "access";

        private static readonly Claim[] Claims =
        [
            new Claim(ClaimTypes.Name, UserName),
            new Claim(ClaimTypes.NameIdentifier, Subject),
            new Claim("sub", Subject),
            new Claim(ClaimTypes.Role, AdminRole),
            new Claim("role", AdminRole),
            new Claim(AccessClaimType, AdminRole)
        ];

        public static ClaimsPrincipal CreatePrincipal(string authenticationType)
        {
            ClaimsIdentity identity = new(Claims, authenticationType, ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
    }

    private sealed class MockAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            ClaimsPrincipal principal = MockAuthenticationDefaults.CreatePrincipal(Scheme.Name);
            AuthenticationTicket ticket = new(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class MockUsersService : IUsersService
    {
        private static readonly UserDto PreviewUser = new()
        {
            Id = "preview-user",
            UserName = "preview-user",
            FirstName = "Preview",
            LastName = "User",
            Email = "preview@example.com"
        };

        private HashSet<string> previewRoles = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<UserDto> Users { get; private set; } = [PreviewUser];
        public UserDto? User { get; private set; } = PreviewUser;

        public Task<bool> CheckUserRole(string roleName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(previewRoles.Contains(roleName));
        }

        public Task Initialize(string[] roles)
        {
            previewRoles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
            Users = [PreviewUser];
            User = PreviewUser;
            return Task.CompletedTask;
        }
    }

    private sealed class MockIdentityService : IIdentityService
    {
        public ClaimsPrincipal? User { get; set; }
    }

    private sealed class MockPersonalAccessTokenClient : IPersonalAccessTokenClient
    {
        public Task<string?> CreateTokenAsync(
            string userId,
            int lifetimeDays = 30,
            IEnumerable<string>? scopes = null,
            bool isReferenceToken = false,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>("preview-token");
        }
    }

    private sealed class MockPersonalClientFactory(
        IOptions<IdentityClientOptions> options,
        IIdentityService identityService,
        IHttpClientFactory httpClientFactory)
        : IPersonalClientFactory
    {
        public Task<HttpClient> CreateAuthenticatedClient(string? endpoint = null)
        {
            identityService.User ??= MockAuthenticationDefaults.CreatePrincipal(MockAuthenticationDefaults.Scheme);
            string baseAddress = endpoint is null
                ? options.Value.Authority
                : UrlExtensions.UrlCombine(options.Value.Authority, endpoint);
            HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
            httpClient.BaseAddress = new Uri(baseAddress);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "preview-token");
            return Task.FromResult(httpClient);
        }
    }

    private sealed class MockIdentityClientService(
        IOptions<IdentityClientOptions> options,
        IHttpClientFactory httpClientFactory)
        : IIdentityClientService
    {
        public Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null)
        {
            string baseAddress = endpoint is null
                ? options.Value.Authority
                : UrlExtensions.UrlCombine(options.Value.Authority, endpoint);
            HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
            httpClient.BaseAddress = new Uri(baseAddress);
            return Task.FromResult(httpClient);
        }

        public Task<string> GetAccessTokenAsync()
        {
            return Task.FromResult("preview-token");
        }
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            return Task.FromResult(response);
        }
    }
}
