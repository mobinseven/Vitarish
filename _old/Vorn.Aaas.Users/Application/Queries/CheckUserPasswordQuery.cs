using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Queries;

public record CheckUserPasswordQuery(string Id, string Password) : IRequest<bool>;

public class CheckUserPasswordQueryValidator : AbstractValidator<CheckUserPasswordQuery>
{
    public CheckUserPasswordQueryValidator()
    {
        RuleFor(q => q.Id).NotEmpty();
        RuleFor(q => q.Password).NotEmpty();
    }
}

internal class CheckUserPasswordQueryHandler(UserManager<User> userManager) : IRequestHandler<CheckUserPasswordQuery, bool>
{
    public async Task<bool> Handle(CheckUserPasswordQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        return user is not null && await userManager.CheckPasswordAsync(user, request.Password);
    }
}
