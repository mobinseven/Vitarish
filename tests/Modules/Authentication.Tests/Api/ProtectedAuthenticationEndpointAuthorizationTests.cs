using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Api.DependencyInjection;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Contracts.Authorization;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Authentication.Tests.Api;

public class ProtectedAuthenticationEndpointAuthorizationTests
{
    public static TheoryData<string> ProtectedEndpointPatterns => new()
    {
        AuthenticationRoutes.Session,
        AuthenticationRoutes.Me,
        AuthenticationRoutes.LogoutContext,
        AuthenticationRoutes.Logout,
        AuthenticationRoutes.BasePath + AuthenticationRoutes.AccessHistory,
        AuthenticationRoutes.BasePath + AuthenticationRoutes.AccessHistoryAcknowledge,
        AuthenticationRoutes.BasePath + AuthenticationRoutes.SessionNotices,
        AuthenticationRoutes.BasePath + AuthenticationRoutes.SessionNoticeAcknowledge,
        AuthenticationRoutes.BasePath + AuthenticationRoutes.SecuritySessionPolicy,
        AuthenticationRoutes.BasePath + AuthenticationRoutes.SecurityUserInactivityPolicy
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpointPatterns))]
    public void Protected_authentication_endpoints_use_browser_or_bearer_group_policy(string routePattern)
    {
        using WebApplication app = CreateAppWithProtectedAuthenticationEndpoints();
        RouteEndpoint[] endpoints = FindRouteEndpoints(app, routePattern);

        endpoints.Should().AllSatisfy(endpoint =>
        {
            AuthorizeAttribute[] authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToArray();
            authorizeMetadata.Should().Contain(attribute =>
                attribute.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer);

            if (IsPlatformAdminProtectedRoute(routePattern))
            {
                authorizeMetadata.Should().Contain(attribute =>
                attribute.Policy == AuthenticationAuthorizationPolicies.ReadSessionPolicy ||
                attribute.Policy == AuthenticationAuthorizationPolicies.ChangeSessionPolicy ||
                attribute.Policy == AuthenticationAuthorizationPolicies.ChangeUserInactivityPolicy);
            }
        });
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpointPatterns))]
    public void Protected_authentication_endpoints_do_not_add_default_only_authorization_metadata(string routePattern)
    {
        using WebApplication app = CreateAppWithProtectedAuthenticationEndpoints();
        RouteEndpoint[] endpoints = FindRouteEndpoints(app, routePattern);

        endpoints.Should().AllSatisfy(endpoint => endpoint.Metadata.OfType<AuthorizeAttribute>()
            .Should().Contain(attribute =>
                attribute.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer));
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
    public void Product_evaluated_profile_maps_only_enabled_login_methods()
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
        protectedGroup.MapAuthenticationSecurityEndpoints();
        return app;
    }

    private static RouteEndpoint FindRouteEndpoint(WebApplication app, string routePattern)
    {
        return FindRouteEndpoints(app, routePattern).Should().ContainSingle().Which;
    }

    private static RouteEndpoint[] FindRouteEndpoints(WebApplication app, string routePattern)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
                endpoint.RoutePattern.RawText == routePattern ||
                endpoint.RoutePattern.RawText == routePattern[AuthenticationRoutes.BasePath.Length..])
            .ToArray();
    }

    private static bool IsPlatformAdminProtectedRoute(string routePattern) =>
        routePattern is
            AuthenticationRoutes.BasePath + AuthenticationRoutes.SecuritySessionPolicy or
            AuthenticationRoutes.BasePath + AuthenticationRoutes.SecurityUserInactivityPolicy;
}
