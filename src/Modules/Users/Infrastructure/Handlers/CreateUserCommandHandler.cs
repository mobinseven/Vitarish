using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
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

internal sealed class CreateUserCommandHandler(UserManager<User> userManager, ISender sender, IDistributedCache cache, UsersManagementAudit managementAudit)
    : IRequestHandler<CreateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        bool phoneConversionFailed = false;
        if (request.User is not null && !string.IsNullOrWhiteSpace(request.User.PhoneNumber))
        {
            if (UserPhoneNumberPolicy.TryNormalize(request.User.PhoneNumber, out string? normalizedPhone))
                request.User.PhoneNumber = normalizedPhone;
            else
                phoneConversionFailed = true;
        }

        if (request.User is null || phoneConversionFailed ||
            !UserContactIdentifierPolicy.IsValid(request.User.Email, request.User.PhoneNumber, request.User.FirstName, request.User.LastName))
        {
            await managementAudit.AccountCreationFailedAsync(request.User?.Id, cancellationToken);
            return IdentityResult.Failed(new IdentityError { Code = nameof(UsersCommandFailureCode.InvalidContact) });
        }

        bool emailOwnedByAnotherAccount = request.User.Email is not null &&
            await sender.Send(new CheckLoginIdentifierInUseQuery(request.User.Email, LoginIdentifierKind.Email, request.User.Id), cancellationToken);
        bool phoneOwnedByAnotherAccount = request.User.PhoneNumber is not null &&
            await sender.Send(new CheckLoginIdentifierInUseQuery(request.User.PhoneNumber, LoginIdentifierKind.Phone, request.User.Id), cancellationToken);
        if (emailOwnedByAnotherAccount || phoneOwnedByAnotherAccount)
        {
            await managementAudit.AccountCreationFailedAsync(request.User.Id, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.ContactIdentifierInUse),
                Description = UsersCommandErrorCodes.ContactIdentifierInUseDescription
            });
        }

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(request.User);
        }
        catch (DbUpdateException exception) when (ContactIdentifierUniqueViolation.Matches(exception))
        {
            await managementAudit.AccountCreationFailedAsync(request.User.Id, CancellationToken.None);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.ContactIdentifierInUse),
                Description = UsersCommandErrorCodes.ContactIdentifierInUseDescription
            });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await managementAudit.AccountCreationFailedAsync(request.User.Id, CancellationToken.None);
            throw;
        }

        if (!result.Succeeded)
        {
            await managementAudit.AccountCreationFailedAsync(request.User.Id, cancellationToken);
            return result;
        }

        await managementAudit.AccountCreatedAsync(request.User.Id, CancellationToken.None);
        await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);

        return result;
    }
}
