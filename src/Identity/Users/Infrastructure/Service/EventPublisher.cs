using MediatR;
using Microsoft.Extensions.Logging;

namespace Vitarish.Users.Infrastructure.Service;

public class EventPublisher(ILogger<EventPublisher> logger, IPublisher mediator)
{
    public async Task PublishAsync<T>(T notification, CancellationToken ct = default) where T : INotification
    {
        logger.LogInformation("Publishing {Event}", typeof(T).Name);
        await mediator.Publish(notification, ct);
    }
}
