using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class ConfirmUserContactCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<ConfirmUserContactCommand, bool>
{
    public async Task<bool> Handle(ConfirmUserContactCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return false;

        string contact = request.Contact.Trim();
        if (!user.EmailConfirmed && user.Email is not null && string.Equals(userManager.NormalizeEmail(user.Email), userManager.NormalizeEmail(contact), StringComparison.Ordinal))
            user.EmailConfirmed = true;
        else if (!user.PhoneNumberConfirmed && user.PhoneNumber is not null && string.Equals(user.PhoneNumber, contact, StringComparison.Ordinal))
            user.PhoneNumberConfirmed = true;
        else
            return false;

        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw new InvalidOperationException("The verified contact could not be confirmed.");
        }

        await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Succeeded, SecurityAuditReasonCatalog.SecurityAttributeChanged, cancellationToken);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        return true;
    }
}
