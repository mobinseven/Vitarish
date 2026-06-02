using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public sealed class SearchUsersQueryHandler(UsersDbContext db) : IRequestHandler<SearchUsersQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        string q = request.Query.Trim();

        return string.IsNullOrWhiteSpace(q)
            ? []
            : (IEnumerable<User>)await db.Users
            .AsNoTracking()
            .Where(u =>
                u.FirstName!.Contains(q) ||
                u.LastName!.Contains(q) ||
                u.UserName!.Contains(q) ||
                u.Email!.Contains(q) ||
                u.PhoneNumber!.Contains(q) ||
                u.IdentificationCode!.Contains(q) ||
                u.Id.Contains(q))
            .Take(request.MaxResults)
            .ToListAsync(cancellationToken);
    }
}
