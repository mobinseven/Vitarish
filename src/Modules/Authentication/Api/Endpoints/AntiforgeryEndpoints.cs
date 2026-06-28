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

public static class AntiforgeryEndpoints { public static IEndpointRouteBuilder MapAntiforgeryEndpoints(this IEndpointRouteBuilder endpoints) { endpoints.MapGet("/antiforgery", (IAntiforgery antiforgery, HttpContext context) => { AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context); return Results.Ok(new { requestToken = tokens.RequestToken, headerName = "X-XSRF-TOKEN" }); }).WithName("Authentication_Antiforgery_Get").WithOpenApi(o => { o.OperationId = "getAuthenticationAntiforgery"; o.Summary = "Gets an antiforgery request token."; return o; }); return endpoints; } }
