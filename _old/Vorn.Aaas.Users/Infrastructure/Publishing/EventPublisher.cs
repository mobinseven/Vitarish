using MediatR;
using Microsoft.Extensions.Logging;

namespace Vorn.Aaas.Users.Infrastructure.Publishing;

public class EventPublisher(ILogger<EventPublisher> logger, IPublisher mediator)
{
    public async Task PublishAsync<T>(T notification, CancellationToken ct = default) where T : INotification
    {
        logger.LogInformation("Publishing {Event}", typeof(T).Name);
        await mediator.Publish(notification, ct);
    }
}
