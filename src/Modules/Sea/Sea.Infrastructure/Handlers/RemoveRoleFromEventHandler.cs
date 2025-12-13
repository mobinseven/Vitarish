using Vitarish.Sea.Application.Commands.Roles;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class RemoveRoleFromEventHandler(IEntityRepository<EventRole, EventRoleDescriptor> repository) : IRequestHandler<RemoveRoleFromEventCommand>
{
    public async Task Handle(RemoveRoleFromEventCommand request, CancellationToken cancellationToken)
    {
        RoleType type = request.IsWrite ? RoleType.Write : RoleType.Read;
        await repository.RemoveAsync(new EventRoleDescriptor { EventId = request.EventId, RoleId = request.RoleId, Type = type }, cancellationToken);
    }
}
