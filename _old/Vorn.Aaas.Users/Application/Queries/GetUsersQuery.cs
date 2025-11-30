
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUsersQuery : IRequest<IEnumerable<User>>;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery> { }

internal class GetUsersQueryHandler(UsersDbContext db, IDistributedCache cache) : IRequestHandler<GetUsersQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUsersAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        User[] users = await db.Users.AsNoTracking().ToArrayAsync(cancellationToken);
        await UserCacheExtensions.SetUsersAsync(cache, users, cancellationToken);
        return users;
    }
}
