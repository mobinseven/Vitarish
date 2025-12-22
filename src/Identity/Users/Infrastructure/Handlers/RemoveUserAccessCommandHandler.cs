using MediatR;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class RemoveUserAccessCommandHandler(UsersDbContext usersDbContext) : IRequestHandler<RemoveUserAccessCommand>
{
    public async Task Handle(RemoveUserAccessCommand request, CancellationToken cancellationToken)
    {
        _ = usersDbContext.Remove(request.UserAccess);
        _ = await usersDbContext.SaveChangesAsync(cancellationToken);
    }
}
