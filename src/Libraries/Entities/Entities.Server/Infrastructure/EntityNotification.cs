namespace Vitarish.Entities.Server.Infrastructure;

public sealed record EntityNotification(EntityNotificationArgs Args) : INotification;
