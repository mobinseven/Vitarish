
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record UpdateUserCommand(User User) : IRequest<IdentityResult>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        _ = RuleFor(c => c.User).NotNull();
        _ = RuleFor(c => c.User.Id).NotNull();
    }
}

internal class UpdateUserCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<UpdateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        IdentityResult result = await userManager.UpdateAsync(request.User);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
