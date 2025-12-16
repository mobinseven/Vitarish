using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Entities.Server.SignalR;

public interface IEntityClient
{
    Task EntityNotification(EntityNotificationCore notification);
}
