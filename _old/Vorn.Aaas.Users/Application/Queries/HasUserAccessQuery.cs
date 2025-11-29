using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Infrastructure.Data;

namespace Vorn.Aaas.Users.Application.Queries;

public record HasUserAccessQuery(string UserId, string ClientId) : IRequest<bool>;

public class HasUserAccessQueryValidator : AbstractValidator<HasUserAccessQuery>
{
    public HasUserAccessQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.ClientId).NotEmpty();
    }
}

public class HasUserAccessQueryHandler(UsersDbContext db) : IRequestHandler<HasUserAccessQuery, bool>
{
    public async Task<bool> Handle(
        HasUserAccessQuery request,
        CancellationToken cancellationToken)
    {
        var prefix = request.ClientId + ":";          // compile‑time concatenation
        return await db.UserClaims
            .AsNoTracking()                           // read‑only, faster
            .Where(c =>
                c.UserId == request.UserId &&
                c.ClaimType == AaasConstants.AccessClaim &&
                EF.Functions.Like(c.ClaimValue, prefix + "%")) // LIKE 'cid:%'
            .AnyAsync(cancellationToken);              // 0/1 row, no materialisation
    }
}
