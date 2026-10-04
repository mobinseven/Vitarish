using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckLoginIdentifierInUseQueryHandler(UsersDbContext dbContext, ILookupNormalizer lookupNormalizer)
    : IRequestHandler<CheckLoginIdentifierInUseQuery, bool>
{
    public Task<bool> Handle(CheckLoginIdentifierInUseQuery request, CancellationToken cancellationToken)
    {
        if (request.Kind is LoginIdentifierKind.Email)
        {
            string normalizedEmail = lookupNormalizer.NormalizeEmail(request.Value);
            return dbContext.Users.AsNoTracking().AnyAsync(
                user => (user.NormalizedEmail == normalizedEmail || user.NormalizedUserName == normalizedEmail)
                        && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
                cancellationToken);
        }

        return dbContext.Users.AsNoTracking().AnyAsync(
            user => (user.PhoneNumber == request.Value || user.NormalizedUserName == request.Value)
                    && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
            cancellationToken);
    }
}
