
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record RemoveUserPasswordCommand(string Id) : IRequest<IdentityResult>;

public class RemoveUserPasswordCommandValidator : AbstractValidator<RemoveUserPasswordCommand>
{
    public RemoveUserPasswordCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}

internal class RemoveUserPasswordCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<RemoveUserPasswordCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(RemoveUserPasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        IdentityResult result = await userManager.RemovePasswordAsync(user);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
