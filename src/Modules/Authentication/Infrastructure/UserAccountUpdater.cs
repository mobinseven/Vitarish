using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Application.Security;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class UserAccountUpdater(
    UserManager<User> userManager,
    ISender sender) : IUserAccountUpdater
{
    public async Task<bool> SetVerifiedPhoneNumberAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        IdentityResult result = await sender.Send(new SetVerifiedUserPhoneNumberCommand(userId, phoneNumber), cancellationToken);
        return result.Succeeded;
    }

    public async Task<bool> SetVerifiedEmailAsync(string userId, string email, CancellationToken cancellationToken = default)
    {
        IdentityResult result = await sender.Send(new SetVerifiedUserEmailCommand(userId, email), cancellationToken);
        return result.Succeeded;
    }

    public async Task ConfirmContactAsync(string userId, string contact, CancellationToken cancellationToken = default) =>
        _ = await sender.Send(new ConfirmUserContactCommand(userId, contact), cancellationToken);

    public async Task<PasswordChangeOutcome> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return PasswordChangeOutcome.Failure(PasswordChangeStatus.UserNotFound);

        if (!string.IsNullOrEmpty(currentPassword) && !await userManager.CheckPasswordAsync(user, currentPassword))
            return PasswordChangeOutcome.Failure(PasswordChangeStatus.InvalidCurrentPassword);

        IdentityResult result;
        if (string.IsNullOrEmpty(currentPassword))
        {
            result = await userManager.AddPasswordAsync(user, newPassword);
        }
        else
        {
            result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }
        if (result.Succeeded)
        {
            return PasswordChangeOutcome.Success();
        }

        PasswordPolicyViolation[] violations = result.Errors
            .Select(error => error.Code)
            .Where(code => code.StartsWith(SecurityPolicyPasswordValidator.PasswordPolicyViolationErrorCodePrefix, StringComparison.Ordinal))
            .Select(code => Enum.TryParse(code[SecurityPolicyPasswordValidator.PasswordPolicyViolationErrorCodePrefix.Length..], out PasswordPolicyViolation violation)
                ? (PasswordPolicyViolation?)violation
                : null)
            .Where(violation => violation.HasValue)
            .Select(violation => violation!.Value)
            .Distinct()
            .ToArray();
        if (violations.Length > 0)
            return PasswordChangeOutcome.PolicyFailure(violations);

        return string.IsNullOrEmpty(currentPassword)
            ? PasswordChangeOutcome.Failure(PasswordChangeStatus.Failed)
            : PasswordChangeOutcome.Failure(PasswordChangeStatus.InvalidCurrentPassword);
    }

    public async Task<PasswordChangeOutcome> AddFirstPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return PasswordChangeOutcome.Failure(PasswordChangeStatus.UserNotFound);
        if (await userManager.HasPasswordAsync(user)) return PasswordChangeOutcome.Failure(PasswordChangeStatus.AlreadyHasPassword);

        IdentityResult result = await userManager.AddPasswordAsync(user, newPassword);
        if (result.Succeeded) return PasswordChangeOutcome.Success();

        PasswordPolicyViolation[] violations = result.Errors
            .Select(error => error.Code)
            .Where(code => code.StartsWith(SecurityPolicyPasswordValidator.PasswordPolicyViolationErrorCodePrefix, StringComparison.Ordinal))
            .Select(code => Enum.TryParse(code[SecurityPolicyPasswordValidator.PasswordPolicyViolationErrorCodePrefix.Length..], out PasswordPolicyViolation violation)
                ? (PasswordPolicyViolation?)violation
                : null)
            .Where(violation => violation.HasValue)
            .Select(violation => violation!.Value)
            .Distinct()
            .ToArray();
        if (violations.Length > 0) return PasswordChangeOutcome.PolicyFailure(violations);

        bool passwordNowExists = await userManager.Users.AsNoTracking()
            .AnyAsync(candidate => candidate.Id == userId && candidate.PasswordHash != null, cancellationToken);
        return passwordNowExists
            ? PasswordChangeOutcome.Failure(PasswordChangeStatus.AlreadyHasPassword)
            : PasswordChangeOutcome.Failure(PasswordChangeStatus.Failed);
    }

    public async Task<PasswordChangeOutcome> ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return PasswordChangeOutcome.Failure(PasswordChangeStatus.UserNotFound);

        string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
        if (result.Succeeded)
            return PasswordChangeOutcome.Success();

        PasswordPolicyViolation[] violations = result.Errors
            .Select(error => error.Code)
            .Where(code => code.StartsWith(SecurityPolicyPasswordValidator.PasswordPolicyViolationErrorCodePrefix, StringComparison.Ordinal))
            .Select(code => Enum.TryParse(code[SecurityPolicyPasswordValidator.PasswordPolicyViolationErrorCodePrefix.Length..], out PasswordPolicyViolation violation)
                ? (PasswordPolicyViolation?)violation
                : null)
            .Where(violation => violation.HasValue)
            .Select(violation => violation!.Value)
            .Distinct()
            .ToArray();
        return violations.Length > 0
            ? PasswordChangeOutcome.PolicyFailure(violations)
            : PasswordChangeOutcome.Failure(PasswordChangeStatus.Failed);
    }

    public async Task<bool> SetTwoFactorEnabledAsync(string userId, bool enabled, bool expectedCurrentValue, AuthenticationMethod? expectedChannel, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null || user.TwoFactorEnabled != expectedCurrentValue) return false;

        if (enabled &&
            (!await userManager.HasPasswordAsync(user) ||
                PasswordLoginSecondFactorChannel.Select(AspNetIdentityAuthenticationUserDirectory.ToDescriptor(user))?.Method != expectedChannel))
        {
            return false;
        }

        IdentityResult result = await userManager.SetTwoFactorEnabledAsync(user, enabled);
        return result.Succeeded;
    }

    public async Task<bool> PasswordMatchesAsync(string userId, string password, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> UserHasPasswordAsync(string userId, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await userManager.HasPasswordAsync(user);
    }
}
