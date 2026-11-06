using System.Threading.RateLimiting;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Landing.Api;
using Vitarish.Landing.Application;
using Vitarish.Landing.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Vitarish.Landing.Application.Tests.Api;

public sealed class LandingRateLimitEndpointTests
{
    [Theory]
    [InlineData(LandingApiOperations.ListWorkspaces)]
    [InlineData(LandingApiOperations.IssueWorkspaceHandoff)]
    [InlineData(LandingApiOperations.IssueOnboardingHandoff)]
    [InlineData(LandingApiOperations.RunHomeConversation)]
    [InlineData(LandingApiOperations.DeleteHomeConversation)]
    public void Signed_in_visitor_endpoints_use_the_authenticated_visitor_policy(string operationId)
    {
        using WebApplication app = CreateApp();
        RouteEndpoint endpoint = FindEndpoint(app, operationId);

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
        endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName
            .Should().Be(LandingRateLimitPolicies.AuthenticatedVisitor);
    }

    [Fact]
    public void Public_conversation_keeps_its_separate_anonymous_rate_limit()
    {
        using WebApplication app = CreateApp();
        RouteEndpoint endpoint = FindEndpoint(app, LandingApiOperations.SubmitConversation);

        endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
        endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName
            .Should().Be(LandingRateLimitPolicies.PublicConversation);
    }

    private static WebApplication CreateApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IPartiesService>(Mock.Of<IPartiesService>());
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddSingleton<IRequestContext>(Mock.Of<IRequestContext>());
        builder.Services.AddSingleton<ISender>(Mock.Of<ISender>());
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy(LandingRateLimitPolicies.PublicConversation, _ => RateLimitPartition.GetNoLimiter("anonymous"));
            options.AddPolicy(LandingRateLimitPolicies.PublicCatalog, _ => RateLimitPartition.GetNoLimiter("catalog"));
            options.AddPolicy(LandingRateLimitPolicies.AuthenticatedVisitor, _ => RateLimitPartition.GetNoLimiter("visitor"));
        });
        WebApplication app = builder.Build();
        app.MapLandingApi();
        return app;
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string operationId) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);
}
