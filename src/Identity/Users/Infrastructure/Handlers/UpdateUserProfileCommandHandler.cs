using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserProfileCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<UpdateUserProfileCommand>
{
    public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        User user = await userManager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("User ID is invalid");
        user.ProfilePreferences = request.Preferences;

        if (request.IdentificationCode is not null)
        {
            user.IdentificationCode = UserIdentityNormalization.NormalizeIdentificationCode(request.IdentificationCode);
        }

        if (request.DateOfBirth.HasValue)
        {
            user.DateOfBirth = request.DateOfBirth;
        }

        if (request.BirthCountryCode is not null)
        {
            user.BirthCountryCode = request.BirthCountryCode.Trim();
        }

        if (request.BirthCity is not null)
        {
            user.BirthCity = request.BirthCity.Trim();
        }
        _ = await userManager.UpdateAsync(user);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
    }
}
