using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserEmailInUseQueryHandler(UserManager<User> userManager) : IRequestHandler<CheckUserEmailInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserEmailInUseQuery request, CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking().AnyAsync(
            user => user.Email == request.Email && (request.ExcludeUserId == null || user.Id != request.ExcludeUserId),
            cancellationToken);
}
