using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record UpdateUserClaimsCommand(User User, IEnumerable<Claim> Claims) : IRequest<IdentityResult>;

public class UpdateUserClaimsCommandValidator : AbstractValidator<UpdateUserClaimsCommand>
{
    public UpdateUserClaimsCommandValidator()
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = RuleFor(c => c.Claims).NotNull();
    }
}
