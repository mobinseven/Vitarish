using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserByNameQueryHandler(
    UsersDbContext dbContext,
    ILookupNormalizer lookupNormalizer) : IRequestHandler<GetUserByNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNameQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawUserName))
        {
            throw new ArgumentException("User name is required.", nameof(request));
        }

        string? normalizedUserName = lookupNormalizer.NormalizeName(request.RawUserName);
        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            throw new ArgumentException("User name must normalize to a non-empty value.", nameof(request));
        }

        User? user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken: cancellationToken);
        return user;
    }
}
