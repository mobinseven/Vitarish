namespace Vitarish.Entities.Server.Infrastructure;

public sealed record EntityNotificationCore(EntityNotificationArgs Args) : INotification;
