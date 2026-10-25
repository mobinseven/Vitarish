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
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Application.Security;
using MediatR;

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
        _ = users.MapModulePost(UsersApiRoutes.CreateWithPhone, CreateWithPhoneAsync, UsersApiRoutes.CreateWithPhoneOperation, typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModulePost(UsersApiRoutes.CreateWithEmail, CreateWithEmailAsync, UsersApiRoutes.CreateWithEmailOperation, typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModulePost(UsersApiRoutes.AdministrationCreateWithPhone, CreateAdministrativeWithPhoneAsync, UsersApiRoutes.AdministrationCreateWithPhoneOperation, typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModulePost(UsersApiRoutes.AdministrationCreateWithEmail, CreateAdministrativeWithEmailAsync, UsersApiRoutes.AdministrationCreateWithEmailOperation, typeof(UserSummaryResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModulePut(UsersApiRoutes.ById, UpdateManagedUserAsync, UsersApiRoutes.AdministrationUpdateUserOperation, typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModulePost(UsersApiRoutes.DisableUser, DisableUserAsync, UsersApiRoutes.DisableUserOperation, typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        _ = users.MapModulePost(UsersApiRoutes.EnableUser, EnableUserAsync, UsersApiRoutes.EnableUserOperation, typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        _ = users.MapModuleGet(
            $"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserSecurity}",
            GetUserSecurityDetailsAsync,
            UsersApiRoutes.AdministrationGetUserSecurityOperation,
            typeof(UserSecurityDetailsResponse))
            .RequireAuthorization(UsersAuthorizationPolicies.ReadSecurityState)
            .Produces<UserSecurityDetailsResponse>(StatusCodes.Status503ServiceUnavailable);
        _ = users.MapModulePut(UsersApiRoutes.SetEmail, SetEmailAsync, UsersApiRoutes.SetEmailOperation, typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageAccounts)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModulePost(UsersApiRoutes.AddRole, AddRoleAsync, "UsersAddRole", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.AssignGlobalRole)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModuleGet($"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserRoles}", ListTenantRolesAsync, "UsersAdministrationListTenantRoles", typeof(TenantRoleMembershipResponse[])).RequireAuthorization(UsersAuthorizationPolicies.ReadTenantRoles);
        _ = users.MapModulePut($"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserRole}", GrantTenantRoleAsync, "UsersAdministrationGrantTenantRole", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReplaceTenantRole)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModuleDelete($"{UsersApiRoutes.Administration}{UsersApiRoutes.AdministrationUserRole}", RevokeTenantRoleAsync, "UsersAdministrationRevokeTenantRole", typeof(UsersCommandResponse)).RequireAuthorization(UsersAuthorizationPolicies.ReplaceTenantRole)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModuleGet(UsersApiRoutes.TenantMembers, ListTenantMembersAsync, UsersApiRoutes.ListTenantMembersOperation, typeof(TenantMemberResponse[])).RequireAuthorization(UsersAuthorizationPolicies.ManageTenantMembers);
        _ = users.MapModulePost(UsersApiRoutes.TenantMembers, ProvisionTenantMemberAsync, UsersApiRoutes.ProvisionTenantMemberOperation, typeof(ProvisionTenantMemberResponse)).RequireAuthorization(UsersAuthorizationPolicies.ManageTenantMembers)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        _ = users.MapModuleDelete(UsersApiRoutes.TenantMember, RemoveTenantMemberAsync, UsersApiRoutes.RemoveTenantMemberOperation, typeof(UsersCommandResponse), successStatusCode: StatusCodes.Status200OK).RequireAuthorization(UsersAuthorizationPolicies.ManageTenantMembers)
            .WithMetadata(new SecurityAuditHandledByApplicationMetadata());
        users.MapProfileEndpoints();
        users.MapWebAuthnEndpoints();
        users.MapPatEndpoints();
        return endpoints;
    }

    private static async Task<IResult> SearchUsersAsync([AsParameters] SearchUsersQueryParameters p, IUsersService service, CancellationToken ct) { if (string.IsNullOrWhiteSpace(p.Query)) { return Results.Ok(Array.Empty<UserSummaryResponse>()); } IEnumerable<User> users = await service.SearchUsersAsync(p.Query.Trim(), p.MaxResults ?? 50, ct); return Results.Ok(users.Select(ToSummary).ToArray()); }
    private static async Task<IResult> ListTenantMembersAsync([AsParameters] ListTenantMembersQueryParameters p, IRequestContext requestContext, ISender sender, CancellationToken ct)
    {
        if (!TryGetManageableTenant(requestContext, out _)) return Results.Forbid();
        IReadOnlyList<TenantMemberSummary> members = await sender.Send(new ListTenantMembersQuery(p.Search, p.MaxResults ?? TenantMemberQueryDefaults.MaxResults), ct);
        return Results.Ok(members.Select(ToTenantMemberResponse).ToArray());
    }

    private static async Task<IResult> ProvisionTenantMemberAsync(ProvisionTenantMemberRequest request, IRequestContext requestContext, ISender sender, CancellationToken ct)
    {
        if (!TryGetManageableTenant(requestContext, out _)) return Results.Forbid();
        ProvisionTenantMemberResult result = await sender.Send(new ProvisionTenantMemberCommand(request.UserId, request.Email, request.PhoneNumber, request.FirstName, request.LastName), ct);
        if (result.User is null)
        {
            return result.ErrorCode switch
            {
                UsersCommandErrorCodes.Unauthorized => Results.Forbid(),
                UsersCommandErrorCodes.TenantMemberNotFound => Results.NotFound(),
                UsersCommandErrorCodes.InvalidContact => Results.BadRequest(UsersCommandResponse.Failed([UsersCommandErrorKinds.ForCode(UsersCommandErrorCodes.InvalidContact, UsersCommandErrorCodes.InvalidContactDescription)])),
                UsersCommandErrorCodes.ActiveSessionConflict => Results.Conflict(UsersCommandResponse.Failed([UsersCommandErrorKinds.ForCode(result.ErrorCode, UsersCommandErrorCodes.ActiveSessionConflictDescription)])),
                UsersCommandErrorCodes.TenantMemberConflict => Results.Conflict(UsersCommandResponse.Failed([UsersCommandErrorKinds.ForCode(result.ErrorCode, UsersCommandErrorCodes.TenantMemberConflictDescription)])),
                UsersCommandErrorCodes.ContactIdentifierInUse => Results.Conflict(UsersCommandResponse.Failed([UsersCommandErrorKinds.ForCode(UsersCommandErrorCodes.ContactIdentifierInUse, UsersCommandErrorCodes.ContactIdentifierInUseDescription)])),
                _ => Results.Conflict(UsersCommandResponse.Failed([UsersCommandErrorKinds.ForCode(result.ErrorCode ?? UsersCommandErrorCodes.TenantMemberConflict, result.ErrorCode ?? UsersCommandErrorCodes.TenantMemberConflictDescription)]))
            };
        }

        return Results.Ok(new ProvisionTenantMemberResponse(ToSafeSummary(result.User), result.RoleSuffix, result.Created, result.AlreadyMember));
    }

    private static async Task<IResult> RemoveTenantMemberAsync(string userId, IRequestContext requestContext, ISender sender, CancellationToken ct)
    {
        if (!TryGetManageableTenant(requestContext, out _)) return Results.Forbid();
        return ToCommandResult(await sender.Send(new RemoveTenantMemberCommand(userId), ct));
    }
    private static async Task<IResult> GetUserAsync(string userId, IUsersService service, CancellationToken ct) => await service.GetUserAsync(userId, ct) is { } user ? Results.Ok(ToSummary(user)) : Results.NotFound();
    private static async Task<IResult> CheckEmailAsync([AsParameters] CheckUserEmailInUseQueryParameters p, IUsersService service, CancellationToken ct) => Results.Ok(new UserExistenceResponse(await service.CheckUserEmailInUseAsync(p.Email, p.ExcludeUserId, ct)));
    private static async Task<IResult> CheckPhoneAsync([AsParameters] CheckUserPhoneNumberInUseQueryParameters p, IUsersService service, CancellationToken ct) => Results.Ok(new UserExistenceResponse(await service.CheckUserPhoneNumberInUseAsync(p.PhoneNumber, p.ExcludeUserId, ct)));
    private static async Task<IResult> CheckRoleAsync([AsParameters] CheckUserRoleQueryParameters p, IUsersService service, CancellationToken ct) => Results.Ok(new UserRoleCheckResponse(await service.CheckUserRoleAsync(p.UserId, p.RoleName, ct)));
    private static async Task<IResult> CreateWithPhoneAsync(CreateUserWithPhoneRequest r, IUsersService service, CancellationToken ct) => await service.CreateUserWithPhoneAsync(new CreateUserWithPhoneCommand(r.PhoneNumber, r.FirstName, r.LastName, r.IdentificationCode, r.DateOfBirth, r.BirthCountryCode, r.BirthCity), ct) is { } user ? Results.Ok(ToSummary(user)) : ContactInUseConflict();
    private static async Task<IResult> CreateWithEmailAsync(CreateUserWithEmailRequest r, IUsersService service, CancellationToken ct) => await service.CreateUserWithEmailAsync(new CreateUserWithEmailCommand(r.Email, r.FirstName, r.LastName, r.IdentificationCode, r.DateOfBirth, r.BirthCountryCode, r.BirthCity), ct) is { } user ? Results.Ok(ToSummary(user)) : ContactInUseConflict();
    private static async Task<IResult> CreateAdministrativeWithPhoneAsync(CreateUserWithPhoneRequest r, IUsersService service, CancellationToken ct) => await service.CreateUserWithPhoneAsync(new CreateUserWithPhoneCommand(r.PhoneNumber, r.FirstName, r.LastName, r.IdentificationCode, r.DateOfBirth, r.BirthCountryCode, r.BirthCity, RejectIfAlreadyExists: true), ct) is { } user ? Results.Ok(ToSummary(user)) : ContactInUseConflict();
    private static async Task<IResult> CreateAdministrativeWithEmailAsync(CreateUserWithEmailRequest r, IUsersService service, CancellationToken ct) => await service.CreateUserWithEmailAsync(new CreateUserWithEmailCommand(r.Email, r.FirstName, r.LastName, r.IdentificationCode, r.DateOfBirth, r.BirthCountryCode, r.BirthCity, RejectIfAlreadyExists: true), ct) is { } user ? Results.Ok(ToSummary(user)) : ContactInUseConflict();
    private static async Task<IResult> UpdateManagedUserAsync(string userId, UpdateManagedUserRequest request, ISender sender, CancellationToken ct)
    {
        IdentityResult result = await sender.Send(new UpdateManagedUserCommand(userId, request.Email, request.PhoneNumber, request.FirstName, request.LastName, request.Revision), ct);
        return ToManagedUserUpdateResult(result);
    }

    internal static IResult ToManagedUserUpdateResult(IdentityResult result)
    {
        if (result.Succeeded)
            return Results.Ok(UsersCommandResponse.Success());

        UsersCommandError[] errors = result.Errors.Select(ToManagedUserError).ToArray();
        if (HasFailure(result, UsersCommandFailureCode.UserNotFound))
            return Results.NotFound(UsersCommandResponse.Failed(errors));
        // A contact another account uses is a conflict, as it is when creating a user.
        if (HasFailure(result, UsersCommandFailureCode.ConcurrencyFailure) || HasFailure(result, UsersCommandFailureCode.ContactIdentifierInUse))
            return Results.Conflict(UsersCommandResponse.Failed(errors));
        if (HasFailure(result, UsersCommandFailureCode.Unauthorized))
            return Results.Forbid();
        return Results.BadRequest(UsersCommandResponse.Failed(errors));
    }

    private static bool HasFailure(IdentityResult result, UsersCommandFailureCode code) =>
        result.Errors.Any(error => string.Equals(error.Code, code.ToString(), StringComparison.Ordinal));

    private static async Task<IResult> DisableUserAsync(string userId, ISender sender, CancellationToken ct) =>
        ToAccountStatusResult(await sender.Send(new ChangeUserAccountStatusCommand(userId, Enabled: false), ct));

    private static async Task<IResult> EnableUserAsync(string userId, ISender sender, CancellationToken ct) =>
        ToAccountStatusResult(await sender.Send(new ChangeUserAccountStatusCommand(userId, Enabled: true), ct));

    private static IResult ToAccountStatusResult(IdentityResult result)
    {
        if (result.Succeeded)
            return Results.Ok(UsersCommandResponse.Success());

        UsersCommandError[] errors = result.Errors.Select(error => UsersCommandErrorKinds.ForCode(error.Code, error.Description)).ToArray();
        if (result.Errors.Any(error => string.Equals(error.Code, UsersCommandErrorCodes.UserNotFound, StringComparison.Ordinal)))
            return Results.NotFound(UsersCommandResponse.Failed(errors));
        if (result.Errors.Any(error => string.Equals(error.Code, UsersCommandErrorCodes.Unauthorized, StringComparison.Ordinal)))
            return Results.Forbid();
        if (result.Errors.Any(error => error.Code is UsersCommandErrorCodes.ActiveSessionConflict or UsersCommandErrorCodes.ConcurrencyFailure))
            return Results.Conflict(UsersCommandResponse.Failed(errors));
        return Results.BadRequest(UsersCommandResponse.Failed(errors));
    }

    private static UsersCommandError ToManagedUserError(IdentityError error)
    {
        if (!Enum.TryParse(error.Code, out UsersCommandFailureCode failureCode) || !Enum.IsDefined(failureCode))
            return UsersCommandErrorKinds.ForCode(error.Code, error.Description);

        return failureCode switch
        {
            UsersCommandFailureCode.InvalidContact => new(UsersCommandErrorCodes.InvalidContact, UsersCommandErrorCodes.InvalidContactDescription, UsersCommandErrorKind.InvalidContact),
            UsersCommandFailureCode.ContactIdentifierInUse => new(UsersCommandErrorCodes.ContactIdentifierInUse, UsersCommandErrorCodes.ContactIdentifierInUseDescription, UsersCommandErrorKind.ContactIdentifierInUse),
            UsersCommandFailureCode.UserNotFound => new(UsersCommandErrorCodes.UserNotFound, UsersCommandErrorCodes.UserNotFoundDescription, UsersCommandErrorKind.UserNotFound),
            UsersCommandFailureCode.ConcurrencyFailure => new(UsersCommandErrorCodes.ConcurrencyFailure, UsersCommandErrorCodes.ConcurrencyFailureDescription, UsersCommandErrorKind.ConcurrencyFailure),
            UsersCommandFailureCode.Unauthorized => new(UsersCommandErrorCodes.Unauthorized, UsersCommandErrorCodes.UnauthorizedDescription, UsersCommandErrorKind.Unauthorized)
        };
    }

    private static IResult ContactInUseConflict() =>
        Results.Conflict(UsersCommandResponse.Failed(
            [UsersCommandErrorKinds.ForCode(UsersCommandErrorCodes.ContactIdentifierInUse, UsersCommandErrorCodes.ContactIdentifierInUseDescription)]));
    private static async Task<IResult> GetUserSecurityDetailsAsync(
        string userId,
        int page,
        int pageSize,
        int? authenticationPage,
        ISender sender,
        CancellationToken cancellationToken)
    {
        UserSecurityDetailsQueryResult details;
        try
        {
            details = await sender.Send(new GetUserSecurityDetailsQuery(userId, page, pageSize, authenticationPage), cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (ArgumentException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid user security history query.", detail: "The requested page is invalid.");
        }

        if (!details.IsAuthorized)
            return Results.Forbid();
        if (details.User is null)
            return Results.NotFound();

        List<TenantRoleMembershipResponse> tenantRoles = details.AssignedRoles
            .Select(ParseTenantRole)
            .Where(role => role is not null)
            .Select(role => role!)
            .ToList();
        HashSet<string> tenantRoleNames = details.AssignedRoles
            .Where(TenantRoleDefinitions.IsTenantRoleName)
            .ToHashSet(StringComparer.Ordinal);
        string[] globalRoles = details.AssignedRoles
            .Where(role => !tenantRoleNames.Contains(role))
            .Order(StringComparer.Ordinal)
            .ToArray();
        UserSecurityIdentityResponse identity = new(
            details.User.Id,
            details.User.UserName,
            details.User.FirstName,
            details.User.LastName,
            details.User.Email,
            details.User.PhoneNumber,
            details.User.FullName,
            details.User.ConcurrencyStamp,
            details.User.IsEnabled);
        UserSecurityDetailsResponse response = new(
            identity,
            globalRoles,
            TenantRoleDefinitions.PermittedGlobalRoles.Order(StringComparer.Ordinal).ToArray(),
            tenantRoles,
            details.SecurityHistory is null ? null : MapUserSecurityHistory(details.SecurityHistory),
            details.AuthenticationHistory is null ? null : MapUserSecurityHistory(details.AuthenticationHistory),
            details.SecurityHistoryAvailable,
            details.AuthenticationHistoryAvailable,
            MapAuthenticationMethods(details.AuthenticationMethods!));

        if (!details.SecurityHistoryAvailable || !details.AuthenticationHistoryAvailable)
        {
            return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(response);
    }

    private static UserAuthenticationMethodsResponse MapAuthenticationMethods(UserAuthenticationMethodsResult methods) => new(
        methods.PasswordSet,
        methods.EmailVerified,
        methods.PhoneNumberVerified,
        methods.Passkeys.Select(passkey => new UserPasskeyResponse(passkey.FriendlyName, passkey.RegisteredAt, passkey.LastUsedAt)).ToArray());

    private static UserSecurityHistoryResponse MapUserSecurityHistory(UserSecurityHistoryResult history) => new(
        history.Items.Select(item => new UserSecurityEventResponse(
            item.EventName,
            item.EventCategory,
            item.Outcome,
            item.ActorId,
            item.Operation,
            item.OccurredAt,
            item.ClientInterfaceHost,
            item.IsAuthenticationEvent,
            item.RedactedReason)).ToArray(),
        history.Page,
        history.PageSize,
        history.TotalCount);
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
            return Results.BadRequest(UsersCommandResponse.Failed([UsersCommandErrorKinds.ForCode(UsersCommandErrorCodes.InvalidTenantRole, UsersCommandErrorCodes.InvalidTenantRoleDescription)]));
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
    private static IResult ToCommandResult(IdentityResult result) => result.Succeeded ? Results.Ok(UsersCommandResponse.Success()) : result.Errors.Any(error => error.Code == UsersCommandErrorCodes.ActiveSessionConflict) ? Results.Conflict(UsersCommandResponse.Failed(result.Errors.Select(e => UsersCommandErrorKinds.ForCode(e.Code, e.Description)).ToArray())) : Results.BadRequest(UsersCommandResponse.Failed(result.Errors.Select(e => UsersCommandErrorKinds.ForCode(e.Code, e.Description)).ToArray()));
    private static IResult ToRoleCommandResult(IdentityResult result) => result.Succeeded ? Results.Ok(UsersCommandResponse.Success()) : Results.Conflict(UsersCommandResponse.Failed(result.Errors.Select(e => UsersCommandErrorKinds.ForCode(e.Code, e.Description)).ToArray()));
    private static UserSummaryResponse ToSummary(User user) => new(user.Id, user.UserName, user.FirstName, user.LastName, user.Email, user.PhoneNumber, user.IdentificationCode, user.DateOfBirth, user.BirthCountryCode, user.BirthCity, user.FullName);
    private static UserSummaryResponse ToSafeSummary(User user) => new(user.Id, user.UserName, user.FirstName, user.LastName, user.Email, user.PhoneNumber, null, null, null, null, user.FullName);
    private static TenantMemberResponse ToTenantMemberResponse(TenantMemberSummary member) => new(new UserSummaryResponse(member.Id, member.UserName, member.FirstName, member.LastName, member.Email, member.PhoneNumber, null, null, null, null, member.FullName), member.RoleSuffix);
    private static bool TryGetManageableTenant(IRequestContext requestContext, out Guid tenantId) =>
        Guid.TryParse(requestContext.TenantId, out tenantId) && UsersSecurityAuthorization.CanManageTenantMembers(requestContext.User, tenantId);
}
