using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Application.Commands;

public sealed class DeleteUserCommandHandler(
    IUserAccountDeletionStore store, IAuthenticationActiveSessionQuery sessions,
    IRequestContext context, UsersManagementAudit audit, TimeProvider clock) : IRequestHandler<DeleteUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (!UsersSecurityAuthorization.CanManageAccounts(context.User))
            return await RefuseAsync(request.Id, nameof(UsersCommandFailureCode.Unauthorized), SecurityAuditReasonCatalog.Unauthorized, null, false, cancellationToken);

        UserAccountDeletionSnapshot? account = null;
        try
        {
            await using IUserAccountDeletionScope scope = await store.BeginAsync(request.Id, cancellationToken);
            account = scope.Account;
            if (account is null)
                return await RefuseAsync(request.Id, nameof(UsersCommandFailureCode.UserNotFound), SecurityAuditReasonCatalog.UserNotFound, null, false, cancellationToken);
            if (account.Deleted)
            {
                await audit.AccountDeletedAsync(account.Id, SecurityAuditOutcome.Succeeded, null, SecurityAuditChangeFields.AccountStatusDeleted, SecurityAuditChangeFields.AccountStatusDeleted, CancellationToken.None);
                return IdentityResult.Success;
            }
            UserDeletionRefusal refusal = UserDeletionPolicy.Evaluate(account.Id, context.UserId, request.Revision, account.Revision,
                account.PlatformAdministrator, account.OtherPlatformAdministrators, account.LastTenantOwner);
            if (refusal != UserDeletionRefusal.None)
                return await RefuseAsync(account.Id, refusal.ToString(), Reason(refusal), Status(account), true, cancellationToken);
            if (await sessions.HasActiveSessionAsync(account.Id, cancellationToken))
                return await RefuseAsync(account.Id, nameof(UsersCommandFailureCode.ActiveSessionConflict), SecurityAuditReasonCatalog.ActiveSessionConflict, Status(account), true, cancellationToken);

            await scope.EraseAsync(clock.GetUtcNow().UtcDateTime, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), cancellationToken);
        }
        catch (UserAccountDeletionConcurrencyException)
        {
            return await RefuseAsync(request.Id, nameof(UsersCommandFailureCode.ConcurrencyFailure), SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict,
                account is null ? null : Status(account), true, CancellationToken.None);
        }
        catch (Exception)
        {
            // A lost database connection during commit can leave its result unknown.
            // Never assert an unchanged after-state without confirmation.
            await audit.AccountDeletedAsync(request.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected,
                account is null ? null : Status(account), null, CancellationToken.None);
            throw;
        }
        await audit.AccountDeletedAsync(request.Id, SecurityAuditOutcome.Succeeded, null, Status(account!), SecurityAuditChangeFields.AccountStatusDeleted, CancellationToken.None);
        return IdentityResult.Success;
    }

    private async Task<IdentityResult> RefuseAsync(string id, string code, string reason, string? status, bool conflict, CancellationToken ct)
    {
        await audit.AccountDeletedAsync(id, SecurityAuditOutcome.Denied, reason, status, status, ct, conflict);
        return IdentityResult.Failed(new IdentityError { Code = code, Description = code });
    }

    private static string Status(UserAccountDeletionSnapshot account) => account.Deleted ? SecurityAuditChangeFields.AccountStatusDeleted
        : account.Enabled ? SecurityAuditChangeFields.AccountStatusEnabled : SecurityAuditChangeFields.AccountStatusDisabled;

    private static string Reason(UserDeletionRefusal refusal) => refusal switch
    {
        UserDeletionRefusal.SelfDeletion => SecurityAuditReasonCatalog.SelfDeletion,
        UserDeletionRefusal.LastPlatformAdministrator => SecurityAuditReasonCatalog.LastPlatformAdministrator,
        UserDeletionRefusal.LastOwnerProtection => SecurityAuditReasonCatalog.LastTenantOwner,
        _ => SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict
    };
}

