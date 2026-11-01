using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class GetUserByNormalizedNameQueryHandler(
    UsersDbContext dbContext) : IRequestHandler<GetUserByNormalizedNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNormalizedNameQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NormalizedUserName))
        {
            throw new ArgumentException("Normalized user name is required.", nameof(request));
        }

        User? user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == request.NormalizedUserName, cancellationToken);

        return user;
    }
}
