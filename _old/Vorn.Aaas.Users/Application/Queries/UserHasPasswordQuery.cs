using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Queries;

public record UserHasPasswordQuery(string Id) : IRequest<bool>;

public class UserHasPasswordQueryValidator : AbstractValidator<UserHasPasswordQuery>
{
    public UserHasPasswordQueryValidator()
    {
        RuleFor(q => q.Id).NotEmpty();
    }
}

internal class UserHasPasswordQueryHandler(UserManager<User> userManager) : IRequestHandler<UserHasPasswordQuery, bool>
{
    public async Task<bool> Handle(UserHasPasswordQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        return user is not null && await userManager.HasPasswordAsync(user);
    }
}
