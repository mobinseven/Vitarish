using MediatR;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Modules.Base.Interface.Abstractions;
using Vitarish.Modules.Base.Interface.Models;

namespace Vitarish.Main.Infrastructure.Handlers;

internal sealed class EntityNotificationHandler(IGlobalRenderNotifier renderNotifier) : INotificationHandler<EntityNotification>
{
    public Task Handle(EntityNotification notification, CancellationToken cancellationToken)
    {
        UiReloadNotification reloadNotification = new(
            notification.Args.EntityName,
            notification.Args.Id,
            notification.Args.CorrelationId);

        _ = renderNotifier.Notify(reloadNotification);
        return Task.CompletedTask;
    }
}
