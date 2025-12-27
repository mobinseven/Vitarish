using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserEmailInUseQueryHandler(UserManager<User> userManager, ILookupNormalizer lookupNormalizer)
    : IRequestHandler<CheckUserEmailInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserEmailInUseQuery request, CancellationToken cancellationToken)
    {
        string normalizedEmail = lookupNormalizer.NormalizeEmail(request.Email);
        return userManager.Users.AsNoTracking().AnyAsync(
            user => user.NormalizedEmail == normalizedEmail
                    && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
            cancellationToken);
    }
}
