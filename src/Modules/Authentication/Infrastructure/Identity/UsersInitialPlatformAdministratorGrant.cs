using MediatR;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Authentication.Infrastructure;

public sealed class UsersInitialPlatformAdministratorGrant(ISender sender) : IInitialPlatformAdministratorGrant
{
    public async Task GrantIfUnclaimedAsync(string userId, CancellationToken cancellationToken) =>
        _ = await sender.Send(new GrantInitialPlatformAdministratorCommand(userId), cancellationToken);
}
