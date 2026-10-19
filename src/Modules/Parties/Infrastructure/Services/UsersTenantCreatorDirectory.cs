using MediatR;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class UsersTenantCreatorDirectory(ISender sender) : ITenantCreatorDirectory
{
    public async Task<TenantCreator?> FindAsync(string userId, CancellationToken cancellationToken = default)
    {
        User? user = await sender.Send(new GetUserQuery(userId), cancellationToken);
        return user is null ? null : new TenantCreator(user.Id, user.PhoneNumberConfirmed);
    }
}
