using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record SetUserSecurityStampCommand(User User, string SecurityStamp) : IRequest<IdentityResult>;

public sealed class SetUserSecurityStampCommandValidator : AbstractValidator<SetUserSecurityStampCommand>
{
    public SetUserSecurityStampCommandValidator()
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = RuleFor(c => c.SecurityStamp).NotEmpty();
    }
}
