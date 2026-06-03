using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserPhoneNumberInUseQueryHandler(UsersDbContext dbContext) : IRequestHandler<CheckUserPhoneNumberInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserPhoneNumberInUseQuery request, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().AnyAsync(
            user => user.PhoneNumber == request.PhoneNumber && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
            cancellationToken);
}
