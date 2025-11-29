using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands;

public record RemoveUserPasswordCommand(string Id) : IRequest<IdentityResult>;

public class RemoveUserPasswordCommandValidator : AbstractValidator<RemoveUserPasswordCommand>
{
    public RemoveUserPasswordCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
    }
}

internal class RemoveUserPasswordCommandHandler(UserManager<User> userManager) : IRequestHandler<RemoveUserPasswordCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(RemoveUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }
        return await userManager.RemovePasswordAsync(user);
    }
}
