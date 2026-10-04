using FluentValidation;
using MediatR;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Security;

namespace Vitarish.Users.Application.Commands;

public record UpdateUserProfileCommand(string UserId, Dictionary<string, string> Preferences, string? IdentificationCode = null, DateOnly? DateOfBirth = null, string? BirthCountryCode = null, string? BirthCity = null) : IRequest;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.Preferences).NotNull();
        _ = RuleFor(c => c.IdentificationCode).MaximumLength(UserIdentityNormalization.IdentificationCodeMaxLength);
        _ = RuleFor(c => c.BirthCountryCode).MaximumLength(UserIdentityNormalization.BirthCountryCodeMaxLength);
        _ = RuleFor(c => c.BirthCity).MaximumLength(UserIdentityNormalization.BirthCityMaxLength);
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
        {
            bool invalid = string.IsNullOrWhiteSpace(command.UserId) ||
                command.Preferences is null ||
                (command.IdentificationCode?.Length ?? 0) > UserIdentityNormalization.IdentificationCodeMaxLength ||
                (command.BirthCountryCode?.Length ?? 0) > UserIdentityNormalization.BirthCountryCodeMaxLength ||
                (command.BirthCity?.Length ?? 0) > UserIdentityNormalization.BirthCityMaxLength;
            if (invalid)
                await managementAudit.AccountEditFailedAsync(command.UserId, null, cancellationToken);
        });
    }
}
