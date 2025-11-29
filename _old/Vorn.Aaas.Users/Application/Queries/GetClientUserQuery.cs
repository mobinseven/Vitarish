namespace Vorn.Aaas.Users.Application.Queries;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.ReadModel.Dtos;

public record GetClientUserQuery(string ClientId, string UserId) : IRequest<UserDto?>;

public class GetClientUserQueryValidator : AbstractValidator<GetClientUserQuery>
{
    public GetClientUserQueryValidator()
    {
        RuleFor(q => q.ClientId).NotEmpty();
        RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetClientUserQueryHandler(UsersDbContext db) : IRequestHandler<GetClientUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetClientUserQuery request,
        CancellationToken cancellationToken)
    {
        // prefix the access‑claim value once; no per‑row Split()
        var prefix = request.ClientId + ":";

        return await (
            from u in db.Users.AsNoTracking()                         // read‑only :contentReference[oaicite:0]{index=0}
            join c in db.UserClaims.AsNoTracking()
                     on u.Id equals c.UserId
            where u.Id == request.UserId
                  && c.ClaimType == AaasConstants.AccessClaim
                  && EF.Functions.Like(c.ClaimValue, prefix + "%")    // LIKE 'cid:%' :contentReference[oaicite:1]{index=1}
            select new UserDto                                       // project directly to DTO :contentReference[oaicite:2]{index=2}
            {
                Id = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AccessClaim = new ClaimDto(c.ClaimType, c.ClaimValue),
                ProfilePreferences = u.ProfilePreferences
            })
            .SingleOrDefaultAsync(cancellationToken);                // 0/1 row by design :contentReference[oaicite:3]{index=3}
    }

}
