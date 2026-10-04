using MediatR;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public sealed record GetUserSecurityDetailsQuery(string UserId, int Page, int PageSize)
    : IRequest<UserSecurityDetailsQueryResult>;

public sealed record UserSecurityDetailsQueryResult(
    bool IsAuthorized,
    User? User,
    IReadOnlyList<string> AssignedRoles,
    UserSecurityHistoryResult? SecurityHistory,
    bool SecurityHistoryAvailable,
    UserSecurityHistoryResult? AuthenticationHistory,
    bool AuthenticationHistoryAvailable);

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
    ISecurityAuditQueryService auditQuery) : IRequestHandler<GetUserSecurityDetailsQuery, UserSecurityDetailsQueryResult>
{
    public async Task<UserSecurityDetailsQueryResult> Handle(
        GetUserSecurityDetailsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UsersSecurityAuthorization.CanReadSecurityState(requestContext.User))
            return new(false, null, [], null, false, null, false);

        User? user = await usersService.GetUserAsync(request.UserId, cancellationToken);
        if (user is null)
            return new(true, null, [], null, false, null, false);

        IEnumerable<Microsoft.AspNetCore.Identity.IdentityRole> roles =
            await usersService.GetUserRolesAsync(user.Id, cancellationToken);
        UserSecurityDetailsQueryResult result = new(
            true,
            user,
            roles.Select(role => role.Name).Where(name => name is not null).Select(name => name!).ToArray(),
            null,
            false,
            null,
            false);

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
            return new(false, null, [], null, false, null, false);
        }

        try
        {
            SecurityAuditPageReadDto authenticationHistory = await auditQuery.QueryPlatformAsync(
                new SecurityAuditPlatformQuery(
                    new SecurityAuditQuery(
                        Page: request.Page,
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
            return new(false, null, [], null, false, null, false);
        }

        return result;
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
