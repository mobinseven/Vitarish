using MediatR;
using Vitarish.Identity.Core.Users.Application.Queries;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

/// <summary>
/// Handler for <see cref="HasUserAccess"/> query.
/// Note: This is a stub implementation. Full implementation requires a UserClaims entity/table.
/// </summary>
internal sealed class HasUserAccessHandler : IRequestHandler<HasUserAccess, bool>
{
    public Task<bool> Handle(HasUserAccess request, CancellationToken cancellationToken)
    {
        // TODO: Implement when UserClaims entity is added to support access claims
        return Task.FromResult(false);
    }
}
