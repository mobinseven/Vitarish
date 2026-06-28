using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class CurrentSessionEndpoints
{
    public static IEndpointRouteBuilder MapCurrentSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/session", async (GetCurrentAuthenticationSessionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetCurrentAuthenticationSessionQuery(), ct);
            return result is AuthenticationResult<Vitarish.Authentication.Application.Abstractions.AuthenticationUserDescriptor?>.Success s
                ? Results.Ok(new AuthenticationSessionDto(s.Value is not null, s.Value is null ? null : new CurrentAuthenticationUserDto(s.Value.UserId, s.Value.DisplayName, s.Value.Email, s.Value.PhoneNumber)))
                : Results.Unauthorized();
        })
        .RequireAuthorization()
        .WithName("Authentication_Session_Get")
        .Produces<AuthenticationSessionDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Gets the current authentication session.");

        endpoints.MapGet("/me", async (GetCurrentAuthenticationSessionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetCurrentAuthenticationSessionQuery(), ct);
            return result is AuthenticationResult<Vitarish.Authentication.Application.Abstractions.AuthenticationUserDescriptor?>.Success s && s.Value is not null
                ? Results.Ok(new CurrentAuthenticationUserDto(s.Value.UserId, s.Value.DisplayName, s.Value.Email, s.Value.PhoneNumber))
                : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("Authentication_Me_Get")
        .Produces<CurrentAuthenticationUserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Gets the current authentication user.");

        return endpoints;
    }
}
