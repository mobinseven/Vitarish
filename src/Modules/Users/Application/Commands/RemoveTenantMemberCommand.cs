using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public sealed record RemoveTenantMemberCommand(string UserId) : IRequest<IdentityResult>;

public sealed class RemoveTenantMemberCommandValidator : AbstractValidator<RemoveTenantMemberCommand>
{
    public RemoveTenantMemberCommandValidator()
    {
        _ = RuleFor(command => command.UserId).NotEmpty();
    }
}
