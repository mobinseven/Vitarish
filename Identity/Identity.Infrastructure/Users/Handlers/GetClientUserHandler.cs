using MediatR;
using Vitarish.Identity.Contracts.Users;
using Vitarish.Identity.Core.Users.Application.Queries;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

/// <summary>
/// Handler for <see cref="GetClientUser"/> query.
/// Note: This is a stub implementation. Full implementation requires a UserClaims entity/table.
/// </summary>
internal sealed class GetClientUserHandler : IRequestHandler<GetClientUser, UserDto?>
{
    public Task<UserDto?> Handle(GetClientUser request, CancellationToken cancellationToken)
    {
        // TODO: Implement when UserClaims entity is added to support access claims
        return Task.FromResult<UserDto?>(null);
    }
}
