using MediatR;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Interfaces.Base.Abstractions;

namespace Vitarish.Main.Infrastructure.Handlers;

internal sealed class EntityNotificationHandler(IRenderNotifier renderNotifier) : INotificationHandler<EntityNotification>
{
    public Task Handle(EntityNotification notification, CancellationToken cancellationToken)
    {
        renderNotifier.Notify(notification.Args);
        return Task.CompletedTask;
    }
}
