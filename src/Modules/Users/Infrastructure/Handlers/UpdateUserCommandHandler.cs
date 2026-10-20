using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Authentication.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserCommandHandler(UsersDbContext dbContext, UserManager<User> userManager, IDistributedCache cache, IAuthenticationActiveSessionQuery activeSessionQuery, UsersManagementAudit managementAudit) : IRequestHandler<UpdateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        User? current;
        try
        {
            current = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == request.User.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(request.User.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        bool phoneConversionFailed = false;
        if (!string.IsNullOrWhiteSpace(request.User.PhoneNumber))
        {
            if (UserPhoneNumberPolicy.TryNormalize(request.User.PhoneNumber, out string? normalizedPhone))
                request.User.PhoneNumber = normalizedPhone;
            else
                phoneConversionFailed = true;
        }

        bool usernameChanged = current is not null &&
            (!string.Equals(current.UserName, request.User.UserName, StringComparison.Ordinal) ||
             !string.Equals(current.NormalizedUserName, request.User.NormalizedUserName, StringComparison.Ordinal));
        bool emailChanged = current is not null &&
            !string.Equals(current.Email, request.User.Email, StringComparison.OrdinalIgnoreCase);
        bool phoneChanged = current is not null &&
            !string.Equals(current.PhoneNumber, request.User.PhoneNumber, StringComparison.Ordinal);
        bool contactIdentifierChanged = emailChanged || phoneChanged;
        bool securityAttributesChanged = usernameChanged || contactIdentifierChanged;

        bool validateEmail = current is null || emailChanged;
        bool validatePhone = current is null || phoneChanged;
        bool validateFirstName = current is null ||
            !string.Equals(current.FirstName, request.User.FirstName, StringComparison.Ordinal);
        bool validateLastName = current is null ||
            !string.Equals(current.LastName, request.User.LastName, StringComparison.Ordinal);

        bool invalidContact =
            (validatePhone && phoneConversionFailed) ||
            (validateEmail && !UserContactIdentifierPolicy.IsValidEmail(request.User.Email)) ||
            (validatePhone && !UserContactIdentifierPolicy.IsValidPhoneNumber(request.User.PhoneNumber)) ||
            (validateFirstName && !UserContactIdentifierPolicy.IsValidName(request.User.FirstName)) ||
            (validateLastName && !UserContactIdentifierPolicy.IsValidName(request.User.LastName)) ||
            ((validateEmail || validatePhone) && !UserContactIdentifierPolicy.HasContactIdentifier(request.User.Email, request.User.PhoneNumber));

        if (invalidContact)
        {
            await managementAudit.AccountEditFailedAsync(request.User.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (securityAttributesChanged)
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.InvalidContact),
                Description = UsersCommandErrorCodes.InvalidContactDescription
            });
        }

        if (usernameChanged)
        {
            bool hasActiveSession;
            try
            {
                hasActiveSession = await activeSessionQuery.HasActiveSessionAsync(request.User.Id, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                await managementAudit.AccountEditFailedAsync(request.User.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
                throw;
            }

            if (hasActiveSession)
            {
                await managementAudit.AccountEditFailedAsync(request.User.Id, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = UsersCommandErrorCodes.ActiveSessionConflict,
                    Description = UsersCommandErrorCodes.ActiveSessionConflictDescription
                });
            }
        }

        User user = request.User;
        IdentityResult result;
        try
        {
            User? stored = await StoredWithChangesAsync(request.User);
            if (stored is null)
            {
                result = IdentityResult.Failed(userManager.ErrorDescriber.ConcurrencyFailure());
            }
            else
            {
                user = stored;
                result = await userManager.UpdateAsync(user);
            }
        }
        catch (DbUpdateException exception) when (ContactIdentifierUniqueViolation.Matches(exception))
        {
            await managementAudit.AccountEditFailedAsync(request.User.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (securityAttributesChanged)
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
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
            await managementAudit.AccountEditFailedAsync(request.User.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            if (securityAttributesChanged)
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (result.Succeeded)
        {
            string? securityAttributeChangeReason = usernameChanged
                ? SecurityAuditReasonCatalog.SecurityAttributeChanged
                : contactIdentifierChanged
                    ? SecurityAuditReasonCatalog.UserContactIdentifierChanged
                    : null;
            await managementAudit.AccountEditedAsync(request.User.Id, securityAttributeChangeReason, CancellationToken.None);
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else
        {
            string reason = result.Errors.Any(error => string.Equals(error.Code, nameof(UsersCommandFailureCode.ConcurrencyFailure), StringComparison.Ordinal))
                ? SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict
                : SecurityAuditReasonCatalog.MutationRejected;
            await managementAudit.AccountEditFailedAsync(request.User.Id, reason, cancellationToken);
            if (securityAttributesChanged)
                await managementAudit.SecurityAttributeChangedAsync(request.User.Id, SecurityAuditOutcome.Denied, reason, cancellationToken);
        }

        return result;
    }

    // Callers may pass a detached copy (the user cache hands one out), while Identity's uniqueness
    // validators load the stored row into Identity's own context, so attaching the copy beside it
    // fails. Apply the copy to the row Identity tracks instead; a copy older than the stored row is
    // refused (null) so it cannot overwrite newer changes.
    private async Task<User?> StoredWithChangesAsync(User requested)
    {
        User? stored = await userManager.FindByIdAsync(requested.Id);
        if (stored is null || ReferenceEquals(stored, requested))
            return stored;
        if (!string.Equals(stored.ConcurrencyStamp, requested.ConcurrencyStamp, StringComparison.Ordinal))
            return null;

        // Copies every mapped property through the Users model; this context does not start tracking either user.
        dbContext.Entry(stored).CurrentValues.SetValues(requested);
        return stored;
    }
}
