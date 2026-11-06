using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Hosting.Infrastructure.Options;

namespace Main.Tests;

public sealed class AccountSecurityRateLimitTests
{
    public static TheoryData<string> PasswordMutationRoutes => new()
    {
        AuthenticationRoutes.ChangePassword,
        AuthenticationRoutes.SetFirstPassword
    };

    [Theory]
    [MemberData(nameof(PasswordMutationRoutes))]
    public async Task Account_security_policy_returns_429_after_its_five_per_minute_limit(string route)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        global::HostPipelineExtensions.AddHostPipeline(builder, new SecurityConfigurationOptions());

        using WebApplication app = builder.Build();
        ApplicationBuilder application = new(app.Services);
        application.UseRateLimiter();
        application.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        RequestDelegate pipeline = application.Build();
        RouteEndpoint endpoint = new(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse(route),
            0,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(AuthenticationRateLimitPolicies.AccountSecurityMutation)),
            nameof(Account_security_policy_returns_429_after_its_five_per_minute_limit));

        HttpContext[] requests = new HttpContext[6];
        for (int index = 0; index < requests.Length; index++)
            requests[index] = await InvokeAsync(pipeline, app.Services, endpoint);

        requests.Take(5).Should().OnlyContain(context => context.Response.StatusCode == StatusCodes.Status204NoContent);
        requests[5].Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<HttpContext> InvokeAsync(RequestDelegate pipeline, IServiceProvider services, Endpoint endpoint)
    {
        using IServiceScope scope = services.CreateScope();
        DefaultHttpContext context = new()
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Method = HttpMethods.Post;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(endpoint);

        await pipeline(context);
        return context;
    }
}
