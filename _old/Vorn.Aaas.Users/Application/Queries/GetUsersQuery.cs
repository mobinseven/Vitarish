using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Data;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUsersQuery : IRequest<IEnumerable<User>>;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery> { }

internal class GetUsersQueryHandler(UsersDbContext db) : IRequestHandler<GetUsersQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await db.Users.AsNoTracking().ToListAsync(cancellationToken);
    }
}
