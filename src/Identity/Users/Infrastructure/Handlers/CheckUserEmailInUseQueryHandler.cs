using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserEmailInUseQueryHandler(UsersDbContext dbContext, ILookupNormalizer lookupNormalizer)
    : IRequestHandler<CheckUserEmailInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserEmailInUseQuery request, CancellationToken cancellationToken)
    {
        string normalizedEmail = lookupNormalizer.NormalizeEmail(request.Email);
        return dbContext.Users.AsNoTracking().AnyAsync(
            user => user.NormalizedEmail == normalizedEmail
                    && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
            cancellationToken);
    }
}
