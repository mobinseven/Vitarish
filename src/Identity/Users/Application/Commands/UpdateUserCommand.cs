using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record UpdateUserCommand(User User) : IRequest<IdentityResult>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = When(c => c.User is not null, () =>
        {
            _ = RuleFor(c => c.User.Id).NotNull();
        });
    }
}
