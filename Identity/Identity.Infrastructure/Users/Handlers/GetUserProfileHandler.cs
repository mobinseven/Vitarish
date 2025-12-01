using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Infrastructure.Users.Data;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

internal sealed class GetUserProfileHandler(UsersDbContext db) : IRequestHandler<GetUserProfile, Dictionary<string, string>>
{
    public async Task<Dictionary<string, string>> Handle(GetUserProfile request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        return user?.ProfilePreferences ?? new Dictionary<string, string>();
    }
}
