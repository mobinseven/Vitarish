using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UserHasPasswordQueryHandler(UsersDbContext dbContext) : IRequestHandler<UserHasPasswordQuery, bool>
{
    public async Task<bool> Handle(UserHasPasswordQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        return user is not null && !string.IsNullOrWhiteSpace(user.PasswordHash);
    }
}
