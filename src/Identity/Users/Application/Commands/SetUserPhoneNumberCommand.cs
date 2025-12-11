using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public record SetUserPhoneNumberCommand(string Id, string PhoneNumber) : IRequest<IdentityResult>;

public class SetUserPhoneNumberCommandValidator : AbstractValidator<SetUserPhoneNumberCommand>
{
    public SetUserPhoneNumberCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}
