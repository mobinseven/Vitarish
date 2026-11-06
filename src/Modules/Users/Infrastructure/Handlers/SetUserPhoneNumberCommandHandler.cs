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

public class SetUserPhoneNumberCommandHandler(
    UserManager<User> userManager,
    ISender sender,
    IDistributedCache cache,
    UsersManagementAudit managementAudit,
    UsersDbContext dbContext) :
    IRequestHandler<SetUserPhoneNumberCommand, IdentityResult>,
    IRequestHandler<SetVerifiedUserPhoneNumberCommand, IdentityResult>
{
    public Task<IdentityResult> Handle(SetUserPhoneNumberCommand request, CancellationToken cancellationToken) =>
        HandleAsync(request.Id, request.PhoneNumber, destinationVerified: false, cancellationToken);

    public Task<IdentityResult> Handle(SetVerifiedUserPhoneNumberCommand request, CancellationToken cancellationToken) =>
        HandleAsync(request.Id, request.PhoneNumber, destinationVerified: true, cancellationToken);

    private async Task<IdentityResult> HandleAsync(string userId, string requestedPhoneNumber, bool destinationVerified, CancellationToken cancellationToken)
    {
        User? user;
        try
        {
            user = await userManager.FindByIdAsync(userId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(userId, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountEditFailedAsync(userId, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        string? previousPhoneNumber = user.PhoneNumber;
        string? phoneNumber = null;
        bool phoneConversionFailed = false;
        if (!string.IsNullOrWhiteSpace(requestedPhoneNumber))
            phoneConversionFailed = !UserPhoneNumberPolicy.TryNormalize(requestedPhoneNumber, out phoneNumber);
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
        bool proposedPhoneConfirmed = destinationVerified || (!changed && user.PhoneNumberConfirmed);
        if (changed && user.TwoFactorEnabled &&
            !UserContactIdentifierPolicy.HasConfirmedContact(user.Email, user.EmailConfirmed, phoneNumber, proposedPhoneConfirmed))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.ConfirmedContactRequiredForTwoStepSignIn),
                Description = UsersCommandErrorCodes.ConfirmedContactRequiredForTwoStepSignInDescription
            });
        }

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

        TrackedUserStateSnapshot snapshot = TrackedUserStateSnapshot.Capture(dbContext, user);
        string? movedUserName = ContactUserName.MoveWithContact(user, user.Email, user.PhoneNumber, user.Email, phoneNumber);
        if (movedUserName is not null)
            user.NormalizedUserName = userManager.NormalizeName(movedUserName);

        user.PhoneNumberConfirmed = proposedPhoneConfirmed;
        user.PhoneNumber = phoneNumber;
        // Identity's setter uses this same generator when rotating the security stamp.
        if (userManager.SupportsUserSecurityStamp)
            user.SecurityStamp = userManager.GenerateNewAuthenticatorKey();

        IdentityResult result;
        try
        {
            // Identity's SetPhoneNumberAsync always clears PhoneNumberConfirmed. Persist
            // the contact, selected confirmation state, and rotated stamp in one update.
            result = await userManager.UpdateAsync(user);
        }
        catch (DbUpdateException exception) when (ContactIdentifierUniqueViolation.Matches(exception))
        {
            snapshot.Restore(dbContext, user);
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
            snapshot.Restore(dbContext, user);
            throw;
        }
        catch
        {
            snapshot.Restore(dbContext, user);
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
                UserAuditChanges.Between(
                    (user.Email, previousPhoneNumber, user.FirstName, user.LastName),
                    (user.Email, user.PhoneNumber, user.FirstName, user.LastName)),
                CancellationToken.None);
            await UserCacheExtensions.RemoveUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else
        {
            snapshot.Restore(dbContext, user);
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        }

        return result;
    }
}
