using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserByNameQuery(string UserName) : IRequest<User?>;

public class GetUserByNameQueryValidator : AbstractValidator<GetUserByNameQuery>
{
    public GetUserByNameQueryValidator()
    {
        RuleFor(q => q.UserName).NotEmpty();
    }
}

internal class GetUserByNameQueryHandler(UserManager<User> userManager) : IRequestHandler<GetUserByNameQuery, User?>
{
    public async Task<User?> Handle(GetUserByNameQuery request, CancellationToken cancellationToken)
    {
        return await userManager.Users.SingleOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);
    }
}
