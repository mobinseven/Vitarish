
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record DeleteUserCommand(string Id) : IRequest<IdentityResult>;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}

internal class DeleteUserCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<DeleteUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        IdentityResult result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await UserCacheExtensions.RemoveUserAsync(cache, user, cancellationToken);
        }

        return result;
    }
}
