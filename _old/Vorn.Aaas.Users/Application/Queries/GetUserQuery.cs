
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserQuery(string Id) : IRequest<User?>;

public class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}

internal class GetUserQueryHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<GetUserQuery, User?>
{
    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
