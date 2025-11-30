
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record AddUserPasswordCommand(string Id, string Password) : IRequest<IdentityResult>;

public class AddUserPasswordCommandValidator : AbstractValidator<AddUserPasswordCommand>
{
    public AddUserPasswordCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Password).NotEmpty();
    }
}

internal class AddUserPasswordCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<AddUserPasswordCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(AddUserPasswordCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        IdentityResult result = await userManager.AddPasswordAsync(user, request.Password);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
