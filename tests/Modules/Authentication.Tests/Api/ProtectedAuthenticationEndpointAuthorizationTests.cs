using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Api.DependencyInjection;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Authentication.Tests.Api;

public class ProtectedAuthenticationEndpointAuthorizationTests
{
    public static TheoryData<string> ProtectedEndpointPatterns => new()
    {
        "/api/modules/authentication/session",
        "/api/modules/authentication/me",
        "/api/modules/authentication/logout/context",
        "/api/modules/authentication/logout"
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpointPatterns))]
    public void Protected_authentication_endpoints_use_browser_or_bearer_group_policy(string routePattern)
    {
        using WebApplication app = CreateAppWithProtectedAuthenticationEndpoints();
        RouteEndpoint endpoint = FindRouteEndpoint(app, routePattern);

        AuthorizeAttribute[] authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToArray();

        authorizeMetadata.Should().ContainSingle();
        authorizeMetadata[0].AuthenticationSchemes.Should().Be(ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpointPatterns))]
    public void Protected_authentication_endpoints_do_not_add_default_only_authorization_metadata(string routePattern)
    {
        using WebApplication app = CreateAppWithProtectedAuthenticationEndpoints();
        RouteEndpoint endpoint = FindRouteEndpoint(app, routePattern);

        endpoint.Metadata.OfType<AuthorizeAttribute>()
            .Should().NotContain(attribute => string.IsNullOrWhiteSpace(attribute.AuthenticationSchemes));
    }

    [Fact]
    public void Antiforgery_bootstrap_has_explicit_anonymous_operation_metadata()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAntiforgery();
        using WebApplication app = builder.Build();
        RouteGroupBuilder anonymous = app.MapAnonymousModuleGroup(
            AuthenticationRoutes.BasePath,
            "Authentication bootstrap is intentionally public.");
        anonymous.MapAntiforgeryEndpoints();

        RouteEndpoint endpoint = FindRouteEndpoint(app, "/api/modules/authentication/antiforgery");

        endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
        endpoint.Metadata.GetMetadata<AnonymousModuleEndpointMetadata>()
            .Should().Be(new AnonymousModuleEndpointMetadata("authentication.antiforgery-bootstrap"));
        endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().BeNull();
    }

    [Fact]
    public void Anonymous_authentication_operations_have_explicit_justified_metadata()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAntiforgery();
        using WebApplication app = builder.Build();
        RouteGroupBuilder anonymous = app.MapAnonymousModuleGroup(
            AuthenticationRoutes.BasePath,
            "Authentication entry points are intentionally public.");
        anonymous.MapPasswordLoginEndpoints();
        anonymous.MapPhoneLoginEndpoints();
        anonymous.MapEmailLoginEndpoints();
        anonymous.MapPasskeyLoginEndpoints();
        anonymous.MapAntiforgeryEndpoints();

        RouteEndpoint[] endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();
        string[] operationIds = endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .Where(name => name?.StartsWith("Authentication_", StringComparison.Ordinal) == true)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        operationIds.Should().Equal(
            "Authentication_Antiforgery_Get",
            "Authentication_Login_Email_Begin",
            "Authentication_Login_Email_Complete",
            "Authentication_Login_Passkey_Begin",
            "Authentication_Login_Passkey_Complete",
            "Authentication_Login_Password",
            "Authentication_Login_Phone_Begin",
            "Authentication_Login_Phone_Complete");
        endpoints.Where(endpoint => operationIds.Contains(
                endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
                StringComparer.Ordinal))
            .Should().AllSatisfy(endpoint =>
            {
                endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
                endpoint.Metadata.GetMetadata<AnonymousModuleEndpointMetadata>()?.Justification.Should().NotBeNullOrWhiteSpace();
                endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().BeNull();
            });
    }

    [Fact]
    public void Anonymous_security_operations_opt_in_to_mandatory_audit()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAntiforgery();
        using WebApplication app = builder.Build();
        RouteGroupBuilder anonymous = app.MapAnonymousModuleGroup(
            AuthenticationRoutes.BasePath,
            "Authentication entry points are intentionally public.");
        anonymous.MapPasswordLoginEndpoints();
        anonymous.MapPhoneLoginEndpoints();
        anonymous.MapEmailLoginEndpoints();
        anonymous.MapPasskeyLoginEndpoints();

        RouteEndpoint[] endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName is
                "Authentication_Login_Email_Begin" or
                "Authentication_Login_Email_Complete" or
                "Authentication_Login_Passkey_Begin" or
                "Authentication_Login_Passkey_Complete" or
                "Authentication_Login_Phone_Begin" or
                "Authentication_Login_Phone_Complete" or
                "Authentication_Login_Password")
            .ToArray();

        endpoints.Should().HaveCount(7);
        endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName != "Authentication_Login_Password")
            .Should().AllSatisfy(endpoint => endpoint.Metadata.GetMetadata<SecurityAuditRequiredMetadata>().Should().NotBeNull());
        endpoints.Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "Authentication_Login_Password")
            .Metadata.GetMetadata<SecurityAuditHandledByApplicationMetadata>().Should().NotBeNull();
    }

    [Fact]
    public void Product_evaluated_profile_maps_only_declared_login_methods()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAntiforgery();
        using WebApplication app = builder.Build();

        app.MapAuthenticationApi(AuthenticationProfilePolicy.ProductEvaluated);

        string[] operationIds = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .Where(name => name?.StartsWith("Authentication_Login_", StringComparison.Ordinal) == true)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        operationIds.Should().Equal(
            "Authentication_Login_Password",
            "Authentication_Login_PasswordOtp_Complete");
    }

    private static WebApplication CreateAppWithProtectedAuthenticationEndpoints()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();
        RouteGroupBuilder protectedGroup = app.MapProtectedModuleGroup(AuthenticationRoutes.BasePath, requireTenantAccess: false);
        protectedGroup.MapCurrentSessionEndpoints();
        protectedGroup.MapLogoutEndpoints();
        return app;
    }

    private static RouteEndpoint FindRouteEndpoint(WebApplication app, string routePattern)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint =>
                endpoint.RoutePattern.RawText == routePattern ||
                endpoint.RoutePattern.RawText == routePattern[AuthenticationRoutes.BasePath.Length..]);
    }
}
