using MediatR;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class AddUserAccessHandler(UsersDbContext usersDb) : IRequestHandler<AddUserAccessCommand>
{
    public async Task Handle(AddUserAccessCommand request, CancellationToken cancellationToken)
    {
        _ = await usersDb.AddAsync(request.UserAccess, cancellationToken);
        _ = await usersDb.SaveChangesAsync(cancellationToken);
    }
}
