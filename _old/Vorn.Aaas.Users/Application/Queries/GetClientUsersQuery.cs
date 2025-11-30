using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.Presentation.Contracts;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetClientUsersQuery(string ClientId) : IRequest<IEnumerable<UserDto>>;

public class GetClientUsersQueryValidator : AbstractValidator<GetClientUsersQuery>
{
    public GetClientUsersQueryValidator()
    {
        _ = RuleFor(q => q.ClientId).NotEmpty();
    }
}

public class GetClientUsersQueryHandler(UsersDbContext db) : IRequestHandler<GetClientUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetClientUsersQuery request, CancellationToken cancellationToken)
    {
        List<UserDto> users = await db.UserClaims
            .AsNoTracking()
            .Where(uc => uc.ClaimType == AaasConstants.AccessClaim)
            .Where(uc => uc.ClaimValue!.StartsWith(request.ClientId))
            .Join(db.Users, uc => uc.UserId, u => u.Id, (uc, u) => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AccessClaim = new ClaimDto(uc.ClaimType, uc.ClaimValue)
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return users.Where(u => u.AccessClaim!.Value!.Split(':').First() == request.ClientId).ToList();
    }
}
