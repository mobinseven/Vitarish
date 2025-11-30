
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserByNameQuery(string UserName) : IRequest<User?>;

public class GetUserByNameQueryValidator : AbstractValidator<GetUserByNameQuery>
{
    public GetUserByNameQueryValidator()
    {
        _ = RuleFor(q => q.UserName).NotEmpty();
    }
}

internal class GetUserByNameQueryHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<GetUserByNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNameQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserByNameAsync(cache, request.UserName, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await userManager.FindByNameAsync(request.UserName);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
