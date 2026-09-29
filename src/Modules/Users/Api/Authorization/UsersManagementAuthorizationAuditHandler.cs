using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Routes;

namespace Vitarish.Users.Api.Authorization;

public sealed class UsersManagementAuthorizationAuditHandler(UsersManagementAudit managementAudit)
    : AuthorizationHandler<RolesAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolesAuthorizationRequirement requirement)
    {
        if (requirement.AllowedRoles.Any(context.User.IsInRole) || context.Resource is not HttpContext httpContext)
            return;

        Endpoint? endpoint = httpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<SecurityAuditHandledByApplicationMetadata>() is null ||
            endpoint.Metadata.GetMetadata<ModuleEndpointOpenApiConvention>() is not { } convention)
        {
            return;
        }

        string? userId = GetSingleRouteValue(httpContext);
        CancellationToken cancellationToken = httpContext.RequestAborted;

        switch (convention.OperationId)
        {
            case UsersApiRoutes.CreateWithPhoneOperation:
            case UsersApiRoutes.CreateWithEmailOperation:
            case UsersApiRoutes.AdministrationCreateWithPhoneOperation:
            case UsersApiRoutes.AdministrationCreateWithEmailOperation:
                await managementAudit.AccountCreationFailedAsync(
                    null,
                    SecurityAuditReasonCatalog.Unauthorized,
                    cancellationToken);
                break;
            case UsersApiRoutes.AdministrationUpdateUserOperation:
                await managementAudit.AccountEditFailedAsync(
                    userId,
                    SecurityAuditReasonCatalog.Unauthorized,
                    cancellationToken);
                break;
            case UsersApiRoutes.SetEmailOperation when !string.IsNullOrWhiteSpace(userId):
                await managementAudit.SecurityAttributeChangedAsync(
                    userId,
                    SecurityAuditOutcome.Denied,
                    SecurityAuditReasonCatalog.Unauthorized,
                    cancellationToken);
                break;
        }
    }

    private static string? GetSingleRouteValue(HttpContext httpContext)
    {
        if (httpContext.GetEndpoint() is not RouteEndpoint routeEndpoint ||
            routeEndpoint.RoutePattern.Parameters.SingleOrDefault()?.Name != UsersApiRoutes.UserIdRouteValue)
            return null;

        return httpContext.Request.RouteValues[UsersApiRoutes.UserIdRouteValue]?.ToString();
    }
}
