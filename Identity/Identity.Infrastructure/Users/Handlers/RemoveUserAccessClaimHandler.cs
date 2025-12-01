using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Application.Commands;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

/// <summary>
/// Handler for <see cref="RemoveUserAccessClaim"/> command.
/// Note: This is a stub implementation. Full implementation requires a UserClaims entity/table.
/// </summary>
internal sealed class RemoveUserAccessClaimHandler : IRequestHandler<RemoveUserAccessClaim, Result>
{
    public Task<Result> Handle(RemoveUserAccessClaim request, CancellationToken cancellationToken)
    {
        // TODO: Implement when UserClaims entity is added to support access claims
        return Task.FromResult(Result.Success());
    }
}
