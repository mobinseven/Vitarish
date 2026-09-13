using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Contracts.Authorization;
using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Contracts.Routes;
using Vitarish.Users.Api.Endpoints;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Api;

public static class UsersApi
{
    public static IEndpointRouteBuilder MapUsersApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder users = endpoints.MapProtectedModuleGroup(UsersApiRoutes.Base, requireTenantAccess: false);
        _ = users.MapModuleGet(UsersApiRoutes.Search, SearchUsersAsync, "UsersSearchUsers", typeof(UserSummaryResponse[])).RequireAuthorization(UsersAuthorizationPolicies.ReadSecurityState);
        _ = users.MapModuleGet(UsersApiRoutes.ById, GetUserAsync, "UsersGetUser", typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReadSecurityState);
        _ = users.MapModuleGet(UsersApiRoutes.CheckEmail, CheckEmailAsync, "UsersCheckEmail", typeof(UserExistenceResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReadSecurityState);
        _ = users.MapModuleGet(UsersApiRoutes.CheckPhone, CheckPhoneAsync, "UsersCheckPhone", typeof(UserExistenceResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReadSecurityState);
        _ = users.MapModuleGet(UsersApiRoutes.CheckRole, CheckRoleAsync, "UsersCheckRole", typeof(UserRoleCheckResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReadSecurityState);
        _ = users.MapModulePost(UsersApiRoutes.CreateWithPhone, CreateWithPhoneAsync, "UsersCreateWithPhone", typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts);
        _ = users.MapModulePost(UsersApiRoutes.CreateWithEmail, CreateWithEmailAsync, "UsersCreateWithEmail", typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts);
        _ = users.MapModulePost(UsersApiRoutes.AdministrationCreateWithPhone, CreateWithPhoneAsync, "UsersAdministrationCreateWithPhone", typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts);
        _ = users.MapModulePost(UsersApiRoutes.AdministrationCreateWithEmail, CreateWithEmailAsync, "UsersAdministrationCreateWithEmail", typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts);
        _ = users.MapModulePut(UsersApiRoutes.SetEmail, SetEmailAsync, "UsersSetEmail", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts);
        _ = users.MapModulePost(UsersApiRoutes.AddRole, AddRoleAsync, "UsersAddRole", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.AssignGlobalRole)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModuleGet($"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserRoles}", ListTenantRolesAsync, "UsersAdministrationListTenantRoles", typeof(TenantRoleMembershipResponse[])).RequireAuthorization(UsersAuthorizationPolicies.ReadTenantRoles);
        _ = users.MapModulePut($"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserRole}", GrantTenantRoleAsync, "UsersAdministrationGrantTenantRole", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReplaceTenantRole)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModuleDelete($"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserRole}", RevokeTenantRoleAsync, "UsersAdministrationRevokeTenantRole", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReplaceTenantRole)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        users.MapProfileEndpoints();
        users.MapWebAuthnEndpoints();
        users.MapPatEndpoints();
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
    private static async Task<IResult> AddRoleAsync(string userId, AddUserToRoleRequest r, IUsersService service, CancellationToken ct) { User? user = await service.GetUserAsync(userId, ct); if (user is null) { return Results.NotFound(); } return ToRoleCommandResult(await service.AddUserToRoleAsync(user, r.RoleName, ct)); }
    private static async Task<IResult> ListTenantRolesAsync(string userId, IUsersService service, CancellationToken ct)
    {
        User? user = await service.GetUserAsync(userId, ct);
        if (user is null) return Results.NotFound();
        IList<string> roles = await service.GetRolesAsync(user, ct);
        return Results.Ok(roles.Select(ParseTenantRole).Where(item => item is not null).Select(item => item!).ToArray());
    }

    private static Task<IResult> GrantTenantRoleAsync(string userId, Guid tenantId, string roleSuffix, [FromBody] ChangeTenantRoleRequest? request, IUsersService service, ITenantExistence tenants, CancellationToken ct) =>
        ChangeTenantRoleAsync(userId, tenantId, roleSuffix, request, service, tenants, grant: true, ct);

    private static Task<IResult> RevokeTenantRoleAsync(string userId, Guid tenantId, string roleSuffix, [FromBody] ChangeTenantRoleRequest? request, IUsersService service, ITenantExistence tenants, CancellationToken ct) =>
        ChangeTenantRoleAsync(userId, tenantId, roleSuffix, request, service, tenants, grant: false, ct);

    private static async Task<IResult> ChangeTenantRoleAsync(string userId, Guid tenantId, string roleSuffix, ChangeTenantRoleRequest? request, IUsersService service, ITenantExistence tenants, bool grant, CancellationToken ct)
    {
        if (request is null || tenantId == Guid.Empty || !IsAllowedTenantRole(roleSuffix) || !string.Equals(request.RoleSuffix, roleSuffix, StringComparison.Ordinal))
            return Results.BadRequest(UsersCommandResponse.Failed([new UsersCommandError(UsersCommandErrorCodes.InvalidTenantRole, UsersCommandErrorCodes.InvalidTenantRoleDescription)]));
        if (!await tenants.ExistsAsync(tenantId, ct)) return Results.NotFound();
        User? user = await service.GetUserAsync(userId, ct);
        if (user is null) return Results.NotFound();
        IdentityResult result = await service.ReplaceTenantRoleAsync(userId, tenantId, grant ? roleSuffix : null, ct);
        return ToRoleCommandResult(result);
    }

    private static TenantRoleMembershipResponse? ParseTenantRole(string role)
    {
        string[] parts = role.Split(TenantRoleDefinitions.RoleSeparator, 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && Guid.TryParse(parts[0], out Guid tenantId) && IsAllowedTenantRole(parts[1])
            ? new TenantRoleMembershipResponse(tenantId, parts[1], true)
            : null;
    }

    private static bool IsAllowedTenantRole(string roleSuffix) =>
        TenantRoleDefinitions.IsPermittedRoleSuffix(roleSuffix);
    private static IResult ToCommandResult(IdentityResult result) => result.Succeeded ? Results.Ok(UsersCommandResponse.Success()) : result.Errors.Any(error => error.Code == UsersCommandErrorCodes.ActiveSessionConflict) ? Results.Conflict(UsersCommandResponse.Failed(result.Errors.Select(e => new UsersCommandError(e.Code, e.Description)).ToArray())) : Results.BadRequest(UsersCommandResponse.Failed(result.Errors.Select(e => new UsersCommandError(e.Code, e.Description)).ToArray()));
    private static IResult ToRoleCommandResult(IdentityResult result) => result.Succeeded ? Results.Ok(UsersCommandResponse.Success()) : Results.Conflict(UsersCommandResponse.Failed(result.Errors.Select(e => new UsersCommandError(e.Code, e.Description)).ToArray()));
    private static UserSummaryResponse ToSummary(User user) => new(user.Id, user.UserName, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.IdentificationCode, user.DateOfBirth, user.BirthCountryCode, user.BirthCity, user.FullName);
}
