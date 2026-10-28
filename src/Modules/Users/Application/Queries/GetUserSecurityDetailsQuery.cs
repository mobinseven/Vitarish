using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public sealed record GetUserSecurityDetailsQuery(string UserId, int Page, int PageSize, int? AuthenticationPage = null)
    : IRequest<UserSecurityDetailsQueryResult>;

public sealed record UserSecurityDetailsQueryResult(
    bool IsAuthorized,
    User? User,
    IReadOnlyList<string> AssignedRoles,
    UserSecurityHistoryResult? SecurityHistory,
    bool SecurityHistoryAvailable,
    UserSecurityHistoryResult? AuthenticationHistory,
    bool AuthenticationHistoryAvailable,
    UserAuthenticationMethodsResult? AuthenticationMethods,
    bool AuthenticationMethodsUnavailable);

public sealed record UserAuthenticationMethodsResult(
    bool PasswordSet,
    bool EmailVerified,
    bool PhoneNumberVerified,
    IReadOnlyList<UserPasskeyEntry> Passkeys,
    EnabledAuthenticationMethods? EnabledMethods = null,
    DateTime? PasswordChangedAt = null);

public sealed record UserPasskeyEntry(
    Guid Id,
    string? FriendlyName,
    DateTime RegisteredAt,
    DateTime? LastUsedAt);

public sealed record UserSecurityHistoryResult(
    IReadOnlyList<UserSecurityHistoryEntry> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record UserSecurityHistoryEntry(
    string EventName,
    string? EventCategory,
    string Outcome,
    string? ActorId,
    string Operation,
    DateTime OccurredAt,
    string? ClientInterfaceHost,
    bool IsAuthenticationEvent,
    string? RedactedReason);

internal sealed class GetUserSecurityDetailsQueryHandler(
    IRequestContext requestContext,
    IUsersService usersService,
    IWebAuthnService webAuthn,
    ISecurityAuditQueryService auditQuery,
    ILogger<GetUserSecurityDetailsQueryHandler> logger,
    IEnabledAuthenticationMethods enabledMethods) : IRequestHandler<GetUserSecurityDetailsQuery, UserSecurityDetailsQueryResult>
{
    private static readonly UserAuthenticationMethodsResult EmptyAuthenticationMethods = new(false, false, false, []);

    public async Task<UserSecurityDetailsQueryResult> Handle(
        GetUserSecurityDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UsersSecurityAuthorization.CanReadSecurityState(requestContext.User))
            return new(false, null, [], null, false, null, false, EmptyAuthenticationMethods, false);

        User? user = await usersService.GetUserAsync(request.UserId, cancellationToken);
        if (user is null)
            return new(true, null, [], null, false, null, false, EmptyAuthenticationMethods, false);

        IEnumerable<Microsoft.AspNetCore.Identity.IdentityRole> roles =
            await usersService.GetUserRolesAsync(user.Id, cancellationToken);

        UserAuthenticationMethodsResult? authenticationMethods = null;
        bool authenticationMethodsUnavailable = false;
        if (IsFirstDetailsLoad(request))
        {
            try
            {
                authenticationMethods = await GetAuthenticationMethodsAsync(user, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to load authentication methods for user {UserId}.", user.Id);
                authenticationMethodsUnavailable = true;
            }
        }

        UserSecurityDetailsQueryResult result = new(
            true,
            user,
            roles.Select(role => role.Name).Where(name => name is not null).Select(name => name!).ToArray(),
            null,
            false,
            null,
            false,
            authenticationMethods,
            authenticationMethodsUnavailable);

        try
        {
            SecurityAuditPageReadDto history = await auditQuery.QueryPlatformAsync(
                new SecurityAuditPlatformQuery(
                    new SecurityAuditQuery(
                        Page: request.Page,
                        PageSize: request.PageSize,
                        SortBy: SecurityAuditSortField.OccurredAt,
                        Descending: true,
                        SubjectId: user.Id)),
                cancellationToken);
            result = result with
            {
                SecurityHistory = MapHistory(history),
                SecurityHistoryAvailable = true
            };
        }
        catch (SecurityAuditStoreException)
        {
            return result;
        }
        catch (UnauthorizedAccessException)
        {
            return new(false, null, [], null, false, null, false, EmptyAuthenticationMethods, false);
        }

        try
        {
            SecurityAuditPageReadDto authenticationHistory = await auditQuery.QueryPlatformAsync(
                new SecurityAuditPlatformQuery(
                    new SecurityAuditQuery(
                        Page: request.AuthenticationPage ?? request.Page,
                        PageSize: request.PageSize,
                        EventCategory: SecurityAuditEventCatalog.AuthenticationCategory,
                        SortBy: SecurityAuditSortField.OccurredAt,
                        Descending: true,
                        SubjectId: user.Id)),
                cancellationToken);
            result = result with
            {
                AuthenticationHistory = MapHistory(authenticationHistory),
                AuthenticationHistoryAvailable = true
            };
        }
        catch (SecurityAuditStoreException)
        {
            return result;
        }
        catch (UnauthorizedAccessException)
        {
            return new(false, null, [], null, false, null, false, EmptyAuthenticationMethods, false);
        }

        return result;
    }

    // History pagination only changes the audit pages; the method inventory belongs to the first details load.
    private static bool IsFirstDetailsLoad(GetUserSecurityDetailsQuery request) =>
        request.Page == 1 && (request.AuthenticationPage ?? 1) == 1;

    // Reports which authentication data the user holds, never the data itself.
    private async Task<UserAuthenticationMethodsResult> GetAuthenticationMethodsAsync(User user, CancellationToken cancellationToken)
    {
        IEnumerable<WebAuthnCredentialDto> credentials = await webAuthn.ListCredentialsAsync(user.Id, cancellationToken);
        EnabledAuthenticationMethods effectiveMethods = await enabledMethods.GetAsync(cancellationToken);
        UserPasswordStatus password = await usersService.GetUserPasswordStatusAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("The user no longer exists.");
        return new(
            password.PasswordSet,
            !string.IsNullOrWhiteSpace(user.Email) && user.EmailConfirmed,
            !string.IsNullOrWhiteSpace(user.PhoneNumber) && user.PhoneNumberConfirmed,
            credentials.Select(credential => new UserPasskeyEntry(credential.Id, credential.FriendlyName, credential.RegisteredAt, credential.LastUsedAt)).ToArray(),
            effectiveMethods,
            password.PasswordChangedAt);
    }

    private static UserSecurityHistoryResult MapHistory(SecurityAuditPageReadDto history) => new(
        history.Items.Select(item => new UserSecurityHistoryEntry(
            item.EventName,
            item.EventCategory,
            item.Outcome,
            item.ActorId,
            item.Operation,
            item.OccurredAt,
            item.ClientInterfaceHost,
            string.Equals(item.EventCategory, SecurityAuditEventCatalog.AuthenticationCategory, StringComparison.Ordinal),
            item.RedactedReason)).ToArray(),
        history.Page,
        history.PageSize,
        history.TotalCount);
}
