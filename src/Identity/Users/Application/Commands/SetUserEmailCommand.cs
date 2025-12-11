using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public record SetUserEmailCommand(string Id, string Email) : IRequest<IdentityResult>;

public class SetUserEmailCommandValidator : AbstractValidator<SetUserEmailCommand>
{
    public SetUserEmailCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
