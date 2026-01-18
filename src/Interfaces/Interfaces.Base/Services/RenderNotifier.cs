using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

[RegisterSingleton]
internal class RenderNotifier : IRenderNotifier
{
    private readonly ConcurrentDictionary<Guid, IRenderNotificationReceiver> observers = [];

    public IDisposable Subscribe(IRenderNotificationReceiver observer)
    {
        if (!observers.ContainsKey(observer.Id))
        {
            _ = observers.TryAdd(observer.Id, observer);
        }
        return new Unsubscriber(observers, observer);
    }

    public async Task TriggerEvent(RenderNotification message) => Parallel.ForEach(observers, (o, CancellationToken) => o.Value.OnNext(message));

    public async Task Notify(object? content)
    {
        RenderNotification renderNotification = new()
        {
            Content = content,
        };

        await TriggerEvent(renderNotification);
    }

    private class Unsubscriber(ConcurrentDictionary<Guid, IRenderNotificationReceiver> observers, IRenderNotificationReceiver observer) : IDisposable
    {
        public void Dispose() => observers.TryRemove(new KeyValuePair<Guid, IRenderNotificationReceiver>(observer.Id, observer));
    }
}
