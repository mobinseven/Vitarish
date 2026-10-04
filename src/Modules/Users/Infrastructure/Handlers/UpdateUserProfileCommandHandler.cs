using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Security;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserProfileCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<UpdateUserProfileCommand>
{
    public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        User? user;
        try
        {
            user = await userManager.FindByIdAsync(request.UserId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(request.UserId, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountEditFailedAsync(request.UserId, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            throw new InvalidDataException("User ID is invalid");
        }

        bool phoneConversionFailed = false;
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            if (UserPhoneNumberPolicy.TryNormalize(user.PhoneNumber, out string? normalizedPhone))
                user.PhoneNumber = normalizedPhone;
            else
                phoneConversionFailed = true;
        }

        if (phoneConversionFailed || !UserContactIdentifierPolicy.IsValid(user.Email, user.PhoneNumber, user.FirstName, user.LastName))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            throw new InvalidDataException(UsersCommandErrorCodes.InvalidContactDescription);
        }

        user.ProfilePreferences = request.Preferences;

        if (request.IdentificationCode is not null)
        {
            user.IdentificationCode = UserIdentityNormalization.NormalizeIdentificationCode(request.IdentificationCode);
        }

        if (request.DateOfBirth.HasValue)
        {
            user.DateOfBirth = request.DateOfBirth;
        }

        if (request.BirthCountryCode is not null)
        {
            user.BirthCountryCode = request.BirthCountryCode.Trim();
        }

        if (request.BirthCity is not null)
        {
            user.BirthCity = request.BirthCity.Trim();
        }
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
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (!result.Succeeded)
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
        await managementAudit.AccountEditedAsync(user.Id, null, CancellationToken.None);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
    }
}
