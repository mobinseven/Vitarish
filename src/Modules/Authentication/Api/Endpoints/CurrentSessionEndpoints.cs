using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Identity;
using Microsoft.AspNetCore.Authorization;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class CurrentSessionEndpoints
{
    public static RouteGroupBuilder MapCurrentSessionEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet("/session", async ([Microsoft.AspNetCore.Mvc.FromServices] GetCurrentAuthenticationSessionHandler handler, HttpContext httpContext, [Microsoft.AspNetCore.Mvc.FromServices] IAuthorizationService authorization, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetCurrentAuthenticationSessionQuery(), ct);
            TenantAccess access = TenantAccessEvaluator.Evaluate(httpContext.User, httpContext.GetTenantId());
            var accessDto = new TenantAccessDto(access.CanAccessTenantOwner, access.CanAccessTenantUser);
            bool canAccessPlatformAdmin = (await authorization.AuthorizeAsync(httpContext.User, IdentityDefinitions.PlatformAdmin)).Succeeded;
            return result is AuthenticationResult<Vitarish.Authentication.Application.Abstractions.AuthenticationUserDescriptor?>.Success s
                ? Results.Ok(new AuthenticationSessionDto(s.Value is not null, s.Value is null ? null : new CurrentAuthenticationUserDto(s.Value.UserId, s.Value.DisplayName, s.Value.GivenName, s.Value.FamilyName, s.Value.Email, s.Value.PhoneNumber), accessDto, canAccessPlatformAdmin))
                : Results.Unauthorized();
        }, "Authentication_Session_Get", typeof(AuthenticationSessionDto))
        .Produces<AuthenticationSessionDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Gets the current authentication session.");

        group.MapModuleGet("/me", async ([Microsoft.AspNetCore.Mvc.FromServices] GetCurrentAuthenticationSessionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetCurrentAuthenticationSessionQuery(), ct);
            return result is AuthenticationResult<Vitarish.Authentication.Application.Abstractions.AuthenticationUserDescriptor?>.Success s && s.Value is not null
                ? Results.Ok(new CurrentAuthenticationUserDto(s.Value.UserId, s.Value.DisplayName, s.Value.GivenName, s.Value.FamilyName, s.Value.Email, s.Value.PhoneNumber))
                : Results.NotFound();
        }, "Authentication_Me_Get", typeof(CurrentAuthenticationUserDto))
        .Produces<CurrentAuthenticationUserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Gets the current authentication user.");

        return group;
    }
}
