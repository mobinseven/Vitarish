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
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class SetUserEmailCommandHandler(
    UserManager<User> userManager,
    ISender sender,
    IDistributedCache cache,
    UsersManagementAudit managementAudit,
    UsersDbContext dbContext) :
    IRequestHandler<SetUserEmailCommand, IdentityResult>,
    IRequestHandler<SetVerifiedUserEmailCommand, IdentityResult>
{
    public Task<IdentityResult> Handle(SetUserEmailCommand request, CancellationToken cancellationToken) =>
        HandleAsync(request.Id, request.Email, destinationVerified: false, cancellationToken);

    public Task<IdentityResult> Handle(SetVerifiedUserEmailCommand request, CancellationToken cancellationToken) =>
        HandleAsync(request.Id, request.Email, destinationVerified: true, cancellationToken);

    private async Task<IdentityResult> HandleAsync(string userId, string requestedEmail, bool destinationVerified, CancellationToken cancellationToken)
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

        string? previousEmail = user.Email;
        string? email = string.IsNullOrWhiteSpace(requestedEmail) ? null : requestedEmail.Trim();
        if (!UserContactIdentifierPolicy.IsValid(email, user.PhoneNumber, user.FirstName, user.LastName))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.InvalidContact,
                Description = UsersCommandErrorCodes.InvalidContactDescription
            });
        }

        bool changed = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        bool proposedEmailConfirmed = destinationVerified || (!changed && user.EmailConfirmed);
        if (changed && user.TwoFactorEnabled &&
            !UserContactIdentifierPolicy.HasConfirmedContact(email, proposedEmailConfirmed, user.PhoneNumber, user.PhoneNumberConfirmed))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.ConfirmedContactRequiredForTwoStepSignIn),
                Description = UsersCommandErrorCodes.ConfirmedContactRequiredForTwoStepSignInDescription
            });
        }

        if (changed && email is not null &&
            await sender.Send(new CheckLoginIdentifierInUseQuery(email, LoginIdentifierKind.Email, user.Id), cancellationToken))
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
        string? movedUserName = ContactUserName.MoveWithContact(user, user.Email, user.PhoneNumber, email, user.PhoneNumber);
        if (movedUserName is not null)
            user.NormalizedUserName = userManager.NormalizeName(movedUserName);

        user.EmailConfirmed = proposedEmailConfirmed;

        user.Email = email;
        user.NormalizedEmail = userManager.NormalizeEmail(email);
        // Identity's setter uses this same generator when rotating the security stamp.
        if (userManager.SupportsUserSecurityStamp)
            user.SecurityStamp = userManager.GenerateNewAuthenticatorKey();

        IdentityResult result;
        try
        {
            // Identity's SetEmailAsync always clears EmailConfirmed. Persist the contact,
            // selected confirmation state, and rotated stamp in one validated update.
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
                    (previousEmail, user.PhoneNumber, user.FirstName, user.LastName),
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
