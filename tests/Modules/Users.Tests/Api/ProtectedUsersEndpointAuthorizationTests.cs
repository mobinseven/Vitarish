using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Modules.Base.Core.Abstractions;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Api;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Contracts.Authorization;
using Vitarish.Users.Contracts.Routes;

namespace Vitarish.Users.Tests.Api;

public sealed class ProtectedUsersEndpointAuthorizationTests
{
    [Theory]
    [InlineData(UsersApiRoutes.DisableUser, UsersApiRoutes.DisableUserOperation)]
    [InlineData(UsersApiRoutes.EnableUser, UsersApiRoutes.EnableUserOperation)]
    public void Account_status_endpoints_require_the_platform_account_policy_and_application_audit(string route, string operationId)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        RegisterEndpointServices(builder);
        using WebApplication app = builder.Build();
        _ = app.MapUsersApi();

        RouteEndpoint endpoint = FindRouteEndpoint(app, UsersApiRoutes.Base + route);

        endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName.Should().Be(operationId);
        AuthorizeAttribute[] authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToArray();
        authorizeMetadata.Should().Contain(attribute => attribute.Policy == UsersAuthorizationPolicies.ManageAccounts);
        authorizeMetadata.Should().Contain(attribute =>
            attribute.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
        endpoint.Metadata.GetMetadata<SecurityAuditHandledByApplicationMetadata>().Should().NotBeNull();
    }

    private static void RegisterEndpointServices(WebApplicationBuilder builder)
    {
        Mock<IRequestContext> requestContext = new();
        _ = builder.Services.AddSingleton(requestContext.Object);
        _ = builder.Services.AddSingleton(new UserSubjectAccessPolicy(requestContext.Object));
        _ = builder.Services.AddSingleton(Mock.Of<IUsersService>());
        _ = builder.Services.AddSingleton(Mock.Of<IUserProfileService>());
        _ = builder.Services.AddSingleton(Mock.Of<IPersonalAccessTokenGenerator>());
        _ = builder.Services.AddSingleton(Mock.Of<IWebAuthnService>());
        _ = builder.Services.AddSingleton(Mock.Of<IValidLocaleProvider>());
        _ = builder.Services.AddSingleton(Mock.Of<ITenantExistence>());
        _ = builder.Services.AddSingleton(Mock.Of<ISender>());
    }

    private static RouteEndpoint FindRouteEndpoint(WebApplication app, string routePattern) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
}
