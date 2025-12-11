using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserPasswordQueryHandler(UserManager<User> userManager) : IRequestHandler<CheckUserPasswordQuery, bool>
{
    public async Task<bool> Handle(CheckUserPasswordQuery request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        return user is not null && await userManager.CheckPasswordAsync(user, request.Password);
    }
}
