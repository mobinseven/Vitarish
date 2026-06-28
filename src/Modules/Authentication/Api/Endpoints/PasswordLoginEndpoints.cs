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

public static class PasswordLoginEndpoints { public static IEndpointRouteBuilder MapPasswordLoginEndpoints(this IEndpointRouteBuilder endpoints) { endpoints.MapPost("/login/password", async (PasswordLoginRequest request, LoginWithPasswordHandler handler, CancellationToken ct) => { var r = await handler.Handle(new LoginWithPasswordCommand(request.UserNameOrEmail, request.Password, request.ReturnUrl, request.RememberMe), ct); return r is AuthenticationResult<LoginResult>.Success s ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value)) : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)r).Error); }).AllowAnonymous().WithName("Authentication_Login_Password").WithOpenApi(o => { o.OperationId = "loginWithPassword"; o.Summary = "Signs in with a password."; return o; }); return endpoints; } }
