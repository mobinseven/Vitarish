using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserPasswordQueryHandler(UsersDbContext dbContext, IPasswordHasher<User> passwordHasher)
    : IRequestHandler<CheckUserPasswordQuery, bool>
{
    public async Task<bool> Handle(CheckUserPasswordQuery request, CancellationToken cancellationToken)
    {
        User? user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
