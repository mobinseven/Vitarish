using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserProfileQueryHandler(UsersDbContext dbContext) : IRequestHandler<GetUserProfileQuery, Dictionary<string, string>>
{
    public async Task<Dictionary<string, string>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        User user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new InvalidDataException("User ID is invalid");
        return user.ProfilePreferences;
    }
}
