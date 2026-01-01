using Vitarish.Sea.Application.Commands.Roles;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class AddRoleToEventHandler(IEntityRepository<EventRole, EventRoleDescriptor> repository) : IRequestHandler<AddRoleToEventCommand>
{
    public async Task Handle(AddRoleToEventCommand request, CancellationToken cancellationToken)
    {
        RoleType type = request.IsWrite ? RoleType.Write : RoleType.Read;
        EventRoleDescriptor descriptor = new()
        { EventId = request.EventId, RoleId = request.RoleId, Type = type };
        if ((await repository.GetAsync(descriptor, cancellationToken)).Count == 0)
        {
            _ = await repository.AddAsync(new EventRole(request.EventId, request.RoleId, type), cancellationToken);
        }
    }
}
