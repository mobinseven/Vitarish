using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserQuery(string Id) : IRequest<User?>;

public class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(q => q.Id).NotEmpty();
    }
}

internal class GetUserQueryHandler(UserManager<User> userManager) : IRequestHandler<GetUserQuery, User?>
{
    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await userManager.Users.SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
    }
}
