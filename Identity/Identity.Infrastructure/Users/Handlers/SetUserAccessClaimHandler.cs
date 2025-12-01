using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Application.Commands;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

/// <summary>
/// Handler for <see cref="SetUserAccessClaim"/> command.
/// Note: This is a stub implementation. Full implementation requires a UserClaims entity/table.
/// </summary>
internal sealed class SetUserAccessClaimHandler : IRequestHandler<SetUserAccessClaim, Result>
{
    public Task<Result> Handle(SetUserAccessClaim request, CancellationToken cancellationToken)
    {
        // TODO: Implement when UserClaims entity is added to support access claims
        return Task.FromResult(Result.Success());
    }
}
