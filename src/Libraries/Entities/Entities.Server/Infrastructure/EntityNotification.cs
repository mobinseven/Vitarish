using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Entities.Server.Infrastructure;

public sealed record EntityNotification(EntityNotificationArgs Args) : INotification;
