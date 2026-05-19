using FluentValidation;
using MediatR;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record CreateUserWithEmailCommand(string Email, string? FirstName, string? LastName, string? IdentificationCode = null, DateOnly? DateOfBirth = null, string? BirthCountryCode = null, string? BirthCity = null) : IRequest<User?>;

public class CreateUserWithEmailCommandValidator : AbstractValidator<CreateUserWithEmailCommand>
{
    public CreateUserWithEmailCommandValidator()
    {
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
        _ = RuleFor(c => c.IdentificationCode).MaximumLength(UserIdentityNormalization.IdentificationCodeMaxLength);
        _ = RuleFor(c => c.BirthCountryCode).MaximumLength(UserIdentityNormalization.BirthCountryCodeMaxLength);
        _ = RuleFor(c => c.BirthCity).MaximumLength(UserIdentityNormalization.BirthCityMaxLength);
    }
}
