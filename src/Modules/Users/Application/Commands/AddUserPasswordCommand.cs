using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public record AddUserPasswordCommand(string Id, string Password) : IRequest<IdentityResult>;

public class AddUserPasswordCommandValidator : AbstractValidator<AddUserPasswordCommand>
{
    public AddUserPasswordCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Password).NotEmpty();
    }
}
