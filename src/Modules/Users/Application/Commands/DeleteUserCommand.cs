using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public record DeleteUserCommand(string Id, string? Revision = null) : IRequest<IdentityResult>;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
