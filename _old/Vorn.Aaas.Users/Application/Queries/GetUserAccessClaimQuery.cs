using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.ReadModel.Dtos;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserAccessClaimQuery(string UserId, string ClientId) : IRequest<ClaimDto?>;

public class GetUserAccessClaimQueryValidator : AbstractValidator<GetUserAccessClaimQuery>
{
    public GetUserAccessClaimQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.ClientId).NotEmpty();
    }
}

public class GetUserAccessClaimQueryHandler(UsersDbContext db) : IRequestHandler<GetUserAccessClaimQuery, ClaimDto?>
{
    public async Task<ClaimDto?> Handle(
        GetUserAccessClaimQuery request,
        CancellationToken cancellationToken)
    {
        string prefix = request.ClientId + ":";
        var claim = await db.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId &&
                c.ClaimType == AaasConstants.AccessClaim &&
                EF.Functions.Like(c.ClaimValue, prefix + "%"))
            .Select(c => new { c.ClaimType, c.ClaimValue })
            .SingleOrDefaultAsync(cancellationToken);

        return claim is null ? null : new ClaimDto(claim.ClaimType, claim.ClaimValue);
    }
}
