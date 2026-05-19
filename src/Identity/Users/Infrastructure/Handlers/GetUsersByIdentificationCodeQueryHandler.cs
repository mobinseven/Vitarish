using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public sealed class GetUsersByIdentificationCodeQueryHandler(UsersDbContext dbContext) : IRequestHandler<GetUsersByIdentificationCodeQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersByIdentificationCodeQuery request, CancellationToken cancellationToken)
    {
        string? normalized = UserIdentityNormalization.NormalizeIdentificationCode(request.IdentificationCode);
        if (normalized is null)
        {
            throw new InvalidDataException("Identification code is invalid.");
        }

        return await dbContext.Users
            .Where(u => u.IdentificationCode == normalized)
            .ToListAsync(cancellationToken);
    }
}
