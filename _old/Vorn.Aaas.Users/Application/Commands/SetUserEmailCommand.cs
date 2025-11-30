
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record SetUserEmailCommand(string Id, string Email) : IRequest<IdentityResult>;

public class SetUserEmailCommandValidator : AbstractValidator<SetUserEmailCommand>
{
    public SetUserEmailCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

internal class SetUserEmailCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<SetUserEmailCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserEmailCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        IdentityResult result = await userManager.SetEmailAsync(user, request.Email);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
