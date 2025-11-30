
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record UpdateUserProfileCommand(string UserId, Dictionary<string, string> Preferences) : IRequest;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.Preferences).NotNull();
    }
}

internal class UpdateUserProfileCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<UpdateUserProfileCommand>
{
    public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        User user = await userManager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("User ID is invalid");
        user.ProfilePreferences = request.Preferences;
        _ = await userManager.UpdateAsync(user);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
    }
}
