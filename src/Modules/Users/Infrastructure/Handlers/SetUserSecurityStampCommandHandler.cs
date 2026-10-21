using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class SetUserSecurityStampCommandHandler(
    UserManager<User> userManager,
    IDistributedCache cache,
    UsersManagementAudit managementAudit) : IRequestHandler<SetUserSecurityStampCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserSecurityStampCommand request, CancellationToken cancellationToken)
    {
        // The caller's copy may come from the user cache, which holds no security stamp,
        // so the stored user is compared and updated.
        User? user = await userManager.FindByIdAsync(request.User.Id);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Code = nameof(UsersCommandFailureCode.UserNotFound), Description = UsersCommandErrorCodes.UserNotFoundDescription });
        bool changed = !string.Equals(user.SecurityStamp, request.SecurityStamp, StringComparison.Ordinal);
        user.SecurityStamp = request.SecurityStamp;
        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(user);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (result.Succeeded)
        {
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Succeeded, SecurityAuditReasonCatalog.SecurityAttributeChanged, CancellationToken.None);
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else if (changed)
            await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);

        return result;
    }
}
