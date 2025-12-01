using MediatR;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

/// <summary>
/// Handler for <see cref="GetClientUsers"/> query.
/// Note: This is a stub implementation. Full implementation requires a UserClaims entity/table.
/// </summary>
internal sealed class GetClientUsersHandler : IRequestHandler<GetClientUsers, IEnumerable<User>>
{
    public Task<IEnumerable<User>> Handle(GetClientUsers request, CancellationToken cancellationToken)
    {
        // TODO: Implement when UserClaims entity is added to support access claims
        return Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
    }
}
