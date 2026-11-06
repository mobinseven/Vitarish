using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Parties.Api;
using Vitarish.Parties.Module.DependencyInjection;
using Xunit;

namespace Parties.Tests.Api;

public sealed class SignInHandoffRateLimitTests
{
    private const string TestSessionCookie = "rate-limit-test-session";

    [Theory]
    [InlineData(ModuleEndpointAuthenticationSchemes.Browser)]
    [InlineData(ModuleEndpointAuthenticationSchemes.Bearer)]
    public async Task Registered_sign_in_handoff_policy_allows_ten_requests_then_rejects_and_isolates_users(string scheme)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = ModuleEndpointAuthenticationSchemes.Browser;
                options.DefaultChallengeScheme = ModuleEndpointAuthenticationSchemes.Browser;
            })
            .AddScheme<AuthenticationSchemeOptions, UserAuthenticationHandler>(ModuleEndpointAuthenticationSchemes.Browser, _ => { })
            .AddScheme<AuthenticationSchemeOptions, UserAuthenticationHandler>(ModuleEndpointAuthenticationSchemes.Bearer, _ => { });
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(options => options.OnRejected = (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return ValueTask.CompletedTask;
        });
        builder.AddParties(options => options.UseInMemoryDatabase("sign-in-handoff-rate-limit"));

        using WebApplication app = builder.Build();
        ApplicationBuilder application = new(app.Services);
        application.UseAuthentication();
        application.UseAuthorization();
        application.UseRateLimiter();
        application.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        RequestDelegate pipeline = application.Build();
        RouteEndpoint endpoint = new(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(
                new AuthorizeAttribute { AuthenticationSchemes = ModuleEndpointAuthenticationSchemes.BrowserOrBearer },
                new EnableRateLimitingAttribute(PartiesRateLimitPolicies.SignInHandoff)),
            "Sign-in handoff rate-limit test endpoint");

        HttpContext[] firstUserRequests = new HttpContext[11];
        for (int index = 0; index < firstUserRequests.Length; index++)
            firstUserRequests[index] = await InvokeAsync(pipeline, app.Services, endpoint, scheme, "first-user");
        HttpContext secondUser = await InvokeAsync(pipeline, app.Services, endpoint, scheme, "second-user");

        for (int index = 0; index < 10; index++)
            firstUserRequests[index].Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        firstUserRequests[10].Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        secondUser.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    private static async Task<HttpContext> InvokeAsync(RequestDelegate pipeline, IServiceProvider services, Endpoint endpoint, string scheme, string userId)
    {
        using IServiceScope scope = services.CreateScope();
        DefaultHttpContext context = new()
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Method = HttpMethods.Get;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Response.Body = new MemoryStream();
        if (scheme == ModuleEndpointAuthenticationSchemes.Bearer)
            context.Request.Headers.Authorization = $"Bearer {userId}";
        else
            context.Request.Headers.Cookie = $"{TestSessionCookie}={userId}";
        context.SetEndpoint(endpoint);

        await pipeline(context);
        return context;
    }

    private sealed class UserAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? userId = Scheme.Name == ModuleEndpointAuthenticationSchemes.Bearer
                ? ReadBearerUserId()
                : Request.Cookies.TryGetValue(TestSessionCookie, out string? cookieUserId) ? cookieUserId : null;
            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult(AuthenticateResult.NoResult());

            ClaimsPrincipal principal = new(new ClaimsIdentity(
                [new Claim(AuthenticatedUserIdResolver.SubjectClaimType, userId)],
                Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }

        private string? ReadBearerUserId()
        {
            const string bearerPrefix = "Bearer ";
            string authorization = Request.Headers.Authorization.ToString();
            return authorization.StartsWith(bearerPrefix, StringComparison.Ordinal)
                ? authorization[bearerPrefix.Length..]
                : null;
        }
    }
}
