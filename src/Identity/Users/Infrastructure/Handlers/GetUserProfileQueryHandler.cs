using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserProfileQueryHandler(UserManager<User> userManager) : IRequestHandler<GetUserProfileQuery, Dictionary<string, string>>
{
    public async Task<Dictionary<string, string>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        User user = await userManager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("User ID is invalid");
        return user.ProfilePreferences;
    }
}
