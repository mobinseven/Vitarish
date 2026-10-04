using FluentValidation;
using MediatR;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Application.Commands;

public record CreateUserWithPhoneCommand(string PhoneNumber, string? FirstName, string? LastName, string? IdentificationCode = null, DateOnly? DateOfBirth = null, string? BirthCountryCode = null, string? BirthCity = null, bool RejectIfAlreadyExists = false) : IRequest<User?>;

public class CreateUserWithPhoneCommandValidator : AbstractValidator<CreateUserWithPhoneCommand>
{
    public CreateUserWithPhoneCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
        _ = RuleFor(c => c.PhoneNumber)
            .Must(phoneNumber => UserPhoneNumberPolicy.TryNormalize(phoneNumber, out _))
            .WithMessage(UserPhoneNumberPolicy.InvalidPhoneNumberMessage);
        _ = RuleFor(c => c.IdentificationCode).MaximumLength(UserIdentityNormalization.IdentificationCodeMaxLength);
        _ = RuleFor(c => c.BirthCountryCode).MaximumLength(UserIdentityNormalization.BirthCountryCodeMaxLength);
        _ = RuleFor(c => c.BirthCity).MaximumLength(UserIdentityNormalization.BirthCityMaxLength);
        _ = RuleFor(c => c.FirstName).MaximumLength(UserContactIdentifierPolicy.MaximumNameLength);
        _ = RuleFor(c => c.LastName).MaximumLength(UserContactIdentifierPolicy.MaximumNameLength);
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
        {
            bool converted = UserPhoneNumberPolicy.TryNormalize(command.PhoneNumber, out string? phoneNumber);
            bool invalid =
                !converted ||
                !UserContactIdentifierPolicy.IsValid(null, phoneNumber, command.FirstName, command.LastName) ||
                (command.IdentificationCode?.Length ?? 0) > UserIdentityNormalization.IdentificationCodeMaxLength ||
                (command.BirthCountryCode?.Length ?? 0) > UserIdentityNormalization.BirthCountryCodeMaxLength ||
                (command.BirthCity?.Length ?? 0) > UserIdentityNormalization.BirthCityMaxLength;
            if (invalid)
                await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
        });
    }
}
