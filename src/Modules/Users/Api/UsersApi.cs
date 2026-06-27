using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Routes;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Api;

public static class UsersApi
{
    public static IEndpointRouteBuilder MapUsersApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder users = endpoints.MapProtectedModuleGroup(UsersApiRoutes.Base);
        _ = users.MapModuleGet(UsersApiRoutes.Search, SearchUsersAsync, "UsersSearchUsers", typeof(UserSummaryResponse[]));
        _ = users.MapModuleGet(UsersApiRoutes.ById, GetUserAsync, "UsersGetUser", typeof(UserSummaryResponse));
        _ = users.MapModuleGet(UsersApiRoutes.CheckEmail, CheckEmailAsync, "UsersCheckEmail", typeof(UserExistenceResponse));
        _ = users.MapModuleGet(UsersApiRoutes.CheckPhone, CheckPhoneAsync, "UsersCheckPhone", typeof(UserExistenceResponse));
        _ = users.MapModuleGet(UsersApiRoutes.CheckRole, CheckRoleAsync, "UsersCheckRole", typeof(UserRoleCheckResponse));
        _ = users.MapModulePost(UsersApiRoutes.CreateWithPhone, CreateWithPhoneAsync, "UsersCreateWithPhone", typeof(UserSummaryResponse));
        _ = users.MapModulePost(UsersApiRoutes.CreateWithEmail, CreateWithEmailAsync, "UsersCreateWithEmail", typeof(UserSummaryResponse));
        _ = users.MapModulePut(UsersApiRoutes.SetEmail, SetEmailAsync, "UsersSetEmail", typeof(UsersCommandResponse));
        _ = users.MapModulePost(UsersApiRoutes.AddRole, AddRoleAsync, "UsersAddRole", typeof(UsersCommandResponse));
        return endpoints;
    }
    private static async Task<IResult> SearchUsersAsync([AsParameters] SearchUsersQueryParameters p, IUsersService service, CancellationToken ct) { if (string.IsNullOrWhiteSpace(p.Query)) { return Results.Ok(Array.Empty<UserSummaryResponse>()); } IEnumerable<User> users = await service.SearchUsersAsync(p.Query.Trim(), p.MaxResults ?? 50, ct); return Results.Ok(users.Select(ToSummary).ToArray()); }
    private static async Task<IResult> GetUserAsync(string userId, IUsersService service, CancellationToken ct) => await service.GetUserAsync(userId, ct) is { } user ? Results.Ok(ToSummary(user)) : Results.NotFound();
    private static async Task<IResult> CheckEmailAsync([AsParameters] CheckUserEmailInUseQueryParameters p, IUsersService service, CancellationToken ct) => Results.Ok(new UserExistenceResponse(await service.CheckUserEmailInUseAsync(p.Email, p.ExcludeUserId, ct)));
    private static async Task<IResult> CheckPhoneAsync([AsParameters] CheckUserPhoneNumberInUseQueryParameters p, IUsersService service, CancellationToken ct) => Results.Ok(new UserExistenceResponse(await service.CheckUserPhoneNumberInUseAsync(p.PhoneNumber, p.ExcludeUserId, ct)));
    private static async Task<IResult> CheckRoleAsync([AsParameters] CheckUserRoleQueryParameters p, IUsersService service, CancellationToken ct) => Results.Ok(new UserRoleCheckResponse(await service.CheckUserRoleAsync(p.UserId, p.RoleName, ct)));
    private static async Task<IResult> CreateWithPhoneAsync(CreateUserWithPhoneRequest r, IUsersService service, CancellationToken ct) => await service.CreateUserWithPhoneAsync(new CreateUserWithPhoneCommand(r.PhoneNumber, r.FirstName, r.LastName, r.IdentificationCode, r.DateOfBirth, r.BirthCountryCode, r.BirthCity), ct) is { } user ? Results.Ok(ToSummary(user)) : Results.Conflict();
    private static async Task<IResult> CreateWithEmailAsync(CreateUserWithEmailRequest r, IUsersService service, CancellationToken ct) => await service.CreateUserWithEmailAsync(new CreateUserWithEmailCommand(r.Email, r.FirstName, r.LastName, r.IdentificationCode, r.DateOfBirth, r.BirthCountryCode, r.BirthCity), ct) is { } user ? Results.Ok(ToSummary(user)) : Results.Conflict();
    private static async Task<IResult> SetEmailAsync(string userId, SetUserEmailRequest r, IUsersService service, CancellationToken ct) => ToCommandResult(await service.SetUserEmailAsync(userId, r.Email, ct));
    private static async Task<IResult> AddRoleAsync(string userId, AddUserToRoleRequest r, IUsersService service, CancellationToken ct) { User? user = await service.GetUserAsync(userId, ct); if (user is null) { return Results.NotFound(); } try { await service.AddToRoleAsync(user, r.RoleName, ct); return Results.Ok(UsersCommandResponse.Success()); } catch (InvalidOperationException ex) { return Results.Conflict(UsersCommandResponse.Failed([new UsersCommandError("RoleAssignmentFailed", ex.Message)])); } }
    private static IResult ToCommandResult(IdentityResult result) => result.Succeeded ? Results.Ok(UsersCommandResponse.Success()) : Results.BadRequest(UsersCommandResponse.Failed(result.Errors.Select(e => new UsersCommandError(e.Code, e.Description)).ToArray()));
    private static UserSummaryResponse ToSummary(User user) => new(user.Id, user.UserName, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.IdentificationCode, user.DateOfBirth, user.BirthCountryCode, user.BirthCity, user.FullName);
}
