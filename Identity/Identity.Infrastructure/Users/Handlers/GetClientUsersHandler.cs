using MediatR;
using Vitarish.Identity.Contracts.Users;
using Vitarish.Identity.Core.Users.Application.Queries;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

/// <summary>
/// Handler for <see cref="GetClientUsers"/> query.
/// Note: This is a stub implementation. Full implementation requires a UserClaims entity/table.
/// </summary>
internal sealed class GetClientUsersHandler : IRequestHandler<GetClientUsers, IEnumerable<UserDto>>
{
    public Task<IEnumerable<UserDto>> Handle(GetClientUsers request, CancellationToken cancellationToken)
    {
        // TODO: Implement when UserClaims entity is added to support access claims
        return Task.FromResult<IEnumerable<UserDto>>(Array.Empty<UserDto>());
    }
}
