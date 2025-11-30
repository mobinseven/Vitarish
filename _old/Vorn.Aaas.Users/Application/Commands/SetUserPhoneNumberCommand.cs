
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record SetUserPhoneNumberCommand(string Id, string PhoneNumber) : IRequest<IdentityResult>;

public class SetUserPhoneNumberCommandValidator : AbstractValidator<SetUserPhoneNumberCommand>
{
    public SetUserPhoneNumberCommandValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}

internal class SetUserPhoneNumberCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<SetUserPhoneNumberCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserPhoneNumberCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        IdentityResult result = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
