using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Security;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class SetUserPhoneNumberCommandHandler(UserManager<User> userManager, ISender sender, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<SetUserPhoneNumberCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserPhoneNumberCommand request, CancellationToken cancellationToken)
    {
        User? user;
        try
        {
            user = await userManager.FindByIdAsync(request.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(request.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountEditFailedAsync(request.Id, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        string? phoneNumber = null;
        bool phoneConversionFailed = false;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            phoneConversionFailed = !UserPhoneNumberPolicy.TryNormalize(request.PhoneNumber, out phoneNumber);
        if (phoneConversionFailed || !UserContactIdentifierPolicy.IsValid(user.Email, phoneNumber, user.FirstName, user.LastName))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.InvalidContact,
                Description = UsersCommandErrorCodes.InvalidContactDescription
            });
        }

        bool changed = !string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal);
        if (changed && phoneNumber is not null &&
            await sender.Send(new CheckLoginIdentifierInUseQuery(phoneNumber, LoginIdentifierKind.Phone, user.Id), cancellationToken))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.ContactIdentifierInUse),
                Description = UsersCommandErrorCodes.ContactIdentifierInUseDescription
            });
        }

        string? movedUserName = ContactUserName.MoveWithContact(user, user.Email, user.PhoneNumber, user.Email, phoneNumber);
        if (movedUserName is not null)
            user.NormalizedUserName = userManager.NormalizeName(movedUserName);

        IdentityResult result;
        try
        {
            result = await userManager.SetPhoneNumberAsync(user, phoneNumber);
        }
        catch (DbUpdateException exception) when (ContactIdentifierUniqueViolation.Matches(exception))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.ContactIdentifierInUse),
                Description = UsersCommandErrorCodes.ContactIdentifierInUseDescription
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (result.Succeeded)
        {
            await managementAudit.AccountEditedAsync(
                user.Id,
                changed ? SecurityAuditReasonCatalog.UserContactIdentifierChanged : null,
                CancellationToken.None);
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        }

        return result;
    }
}
