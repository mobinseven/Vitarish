using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Validation;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class ChangePasswordHandler(IUserAccountUpdater accountUpdater)
{
    public async Task<AuthenticationResult<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!AuthenticationInputValidator.HasText(command.UserId))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "User ID is required."));

        if (!AuthenticationInputValidator.HasText(command.CurrentPassword))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "Current password is required."));

        if (!AuthenticationInputValidator.HasText(command.NewPassword))
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "New password is required."));

        if (command.NewPassword.Length < 6)
            return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidInput, "New password must be at least 6 characters."));

        bool hasPassword = await accountUpdater.UserHasPasswordAsync(command.UserId, cancellationToken);
        if (hasPassword)
        {
            bool currentValid = await accountUpdater.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
            if (!currentValid)
                return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.InvalidCredentials, "Current password is incorrect."));
        }
        else
        {
            bool created = await accountUpdater.ChangePasswordAsync(command.UserId, string.Empty, command.NewPassword, cancellationToken);
            if (!created)
                return new AuthenticationResult<bool>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Failed to set password."));
        }

        return new AuthenticationResult<bool>.Success(true);
    }
}
