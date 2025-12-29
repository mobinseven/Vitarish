using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetCurrentUserQueryHandler(UsersDbContext dbContext, IRequestContext requestContext) : IRequestHandler<GetCurrentUserQuery, User?>
{
    public async Task<User?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        string userId = requestContext.UserId ?? throw new InvalidDataException("IRequestContext provided a null user ID");
        return await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}
