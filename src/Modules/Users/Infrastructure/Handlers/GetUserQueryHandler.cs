using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserQueryHandler(UsersDbContext dbContext) : IRequestHandler<GetUserQuery, User?>
{
    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        return user;
    }
}
