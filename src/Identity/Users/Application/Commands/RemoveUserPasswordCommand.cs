using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Application.Commands;

public record RemoveUserPasswordCommand(string Id) : IRequest<IdentityResult>;

public class RemoveUserPasswordCommandValidator : AbstractValidator<RemoveUserPasswordCommand>
{
    public RemoveUserPasswordCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
