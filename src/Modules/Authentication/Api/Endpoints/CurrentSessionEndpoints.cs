using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class CurrentSessionEndpoints { public static IEndpointRouteBuilder MapCurrentSessionEndpoints(this IEndpointRouteBuilder endpoints) { endpoints.MapGet("/session", async (GetCurrentAuthenticationSessionHandler handler, CancellationToken ct) => await Session(handler, ct)).RequireAuthorization().WithName("Authentication_Session_Get").WithOpenApi(o => { o.OperationId = "getAuthenticationSession"; o.Summary = "Gets the current authentication session."; return o; }); endpoints.MapGet("/me", async (GetCurrentAuthenticationSessionHandler handler, CancellationToken ct) => await Session(handler, ct)).RequireAuthorization().WithName("Authentication_Me_Get").WithOpenApi(o => { o.OperationId = "getCurrentAuthenticationUser"; o.Summary = "Gets the current authentication user."; return o; }); return endpoints; } private static async Task<IResult> Session(GetCurrentAuthenticationSessionHandler handler, CancellationToken ct) { var result = await handler.Handle(new GetCurrentAuthenticationSessionQuery(), ct); return result is AuthenticationResult<Vitarish.Authentication.Application.Abstractions.AuthenticationUserDescriptor?>.Success s ? Results.Ok(new AuthenticationSessionDto(s.Value is not null, s.Value is null ? null : new CurrentAuthenticationUserDto(s.Value.UserId, s.Value.DisplayName, s.Value.Email, s.Value.PhoneNumber))) : Results.Unauthorized(); } }
