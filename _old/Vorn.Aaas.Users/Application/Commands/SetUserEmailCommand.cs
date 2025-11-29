using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands;

public record SetUserEmailCommand(string Id, string Email) : IRequest<IdentityResult>;

public class SetUserEmailCommandValidator : AbstractValidator<SetUserEmailCommand>
{
    public SetUserEmailCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

internal class SetUserEmailCommandHandler(UserManager<User> userManager) : IRequestHandler<SetUserEmailCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }
        return await userManager.SetEmailAsync(user, request.Email);
    }
}
