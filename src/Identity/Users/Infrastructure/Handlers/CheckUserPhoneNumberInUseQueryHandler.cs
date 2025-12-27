using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserPhoneNumberInUseQueryHandler(UserManager<User> userManager) : IRequestHandler<CheckUserPhoneNumberInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserPhoneNumberInUseQuery request, CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking().AnyAsync(
            user => user.PhoneNumber == request.PhoneNumber && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
            cancellationToken);
}
