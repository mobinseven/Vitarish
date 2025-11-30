using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Queries;

public record UserHasPasswordQuery(string Id) : IRequest<bool>;

public class UserHasPasswordQueryValidator : AbstractValidator<UserHasPasswordQuery>
{
    public UserHasPasswordQueryValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}

internal class UserHasPasswordQueryHandler(UserManager<User> userManager) : IRequestHandler<UserHasPasswordQuery, bool>
{
    public async Task<bool> Handle(UserHasPasswordQuery request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        return user is not null && await userManager.HasPasswordAsync(user);
    }
}
