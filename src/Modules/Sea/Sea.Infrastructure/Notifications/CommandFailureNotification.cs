namespace Vitarish.Sea.Infrastructure.Notifications;

public record CommandFailureNotification(string CommandName, Exception Exception) : INotification;
