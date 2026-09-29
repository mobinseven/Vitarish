using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
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
        bool changed = !string.Equals(request.User.SecurityStamp, request.SecurityStamp, StringComparison.Ordinal);
        request.User.SecurityStamp = request.SecurityStamp;
        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(request.User);
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
            await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else if (changed)
            await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);

        return result;
    }
}
