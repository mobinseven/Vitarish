using FluentValidation;
using MediatR;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record CreateUserWithEmailCommand(string Email, string? FirstName, string? LastName, string? IdentificationCode = null, DateOnly? DateOfBirth = null, string? BirthCountryCode = null, string? BirthCity = null, bool RejectIfAlreadyExists = false) : IRequest<User?>;

public class CreateUserWithEmailCommandValidator : AbstractValidator<CreateUserWithEmailCommand>
{
    public CreateUserWithEmailCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
        _ = RuleFor(c => c.Email).MaximumLength(UserContactIdentifierPolicy.MaximumContactLength);
        _ = RuleFor(c => c.FirstName).MaximumLength(UserContactIdentifierPolicy.MaximumNameLength);
        _ = RuleFor(c => c.LastName).MaximumLength(UserContactIdentifierPolicy.MaximumNameLength);
        _ = RuleFor(c => c.IdentificationCode).MaximumLength(UserIdentityNormalization.IdentificationCodeMaxLength);
        _ = RuleFor(c => c.BirthCountryCode).MaximumLength(UserIdentityNormalization.BirthCountryCodeMaxLength);
        _ = RuleFor(c => c.BirthCity).MaximumLength(UserIdentityNormalization.BirthCityMaxLength);
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
        {
            bool invalid =
                !UserContactIdentifierPolicy.IsValid(command.Email, null, command.FirstName, command.LastName) ||
                (command.IdentificationCode?.Length ?? 0) > UserIdentityNormalization.IdentificationCodeMaxLength ||
                (command.BirthCountryCode?.Length ?? 0) > UserIdentityNormalization.BirthCountryCodeMaxLength ||
                (command.BirthCity?.Length ?? 0) > UserIdentityNormalization.BirthCityMaxLength;
            if (invalid)
                await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
        });
    }
}
