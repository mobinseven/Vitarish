using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetCurrentUserQueryHandler(UserManager<User> userManager, IRequestContext requestContext) : IRequestHandler<GetCurrentUserQuery, User?>
{
    public async Task<User?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) => await userManager.FindByIdAsync(requestContext.UserId);
}
