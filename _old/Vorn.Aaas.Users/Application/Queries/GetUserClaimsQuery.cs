using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Presentation.Contracts;

namespace Vorn.Aaas.Users.Application.Queries;

public record GetUserClaimsQuery(string UserId) : IRequest<IEnumerable<ClaimDto>>;

public class GetUserClaimsQueryValidator : AbstractValidator<GetUserClaimsQuery>
{
    public GetUserClaimsQueryValidator()
    {
        _ = RuleFor(q => q.UserId).NotEmpty();
    }
}

public class GetUserClaimsQueryHandler(UserManager<User> manager) : IRequestHandler<GetUserClaimsQuery, IEnumerable<ClaimDto>>
{
    public async Task<IEnumerable<ClaimDto>> Handle(GetUserClaimsQuery request, CancellationToken cancellationToken)
    {
        User user = await manager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("user Id is invalid");
        IList<Claim> claims = await manager.GetClaimsAsync(user);
        return claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList();
    }
}
