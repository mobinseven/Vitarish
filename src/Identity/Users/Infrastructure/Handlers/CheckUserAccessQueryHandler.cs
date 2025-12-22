using MediatR;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class CheckUserAccessQueryHandler(UsersDbContext usersDbContext, IRequestContext requestContext) : IRequestHandler<CheckUserAccessQuery, bool>
{
    public async Task<bool> Handle(CheckUserAccessQuery request, CancellationToken cancellationToken)
    {
        UserAccess userAccess = new()
        {
            ClientId = requestContext.ClientId,
            OwnerId = requestContext.OwnerId,
            TenantId = requestContext.TenantId,
            ResourceId = request.Resource,
            UserId = requestContext.UserId,
        };
        UserAccess? ua = await usersDbContext.FindAsync<UserAccess>(userAccess.Key);
        return ua != null;
    }
}
