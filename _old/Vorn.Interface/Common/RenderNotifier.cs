using System.Collections.Concurrent;

namespace Vorn.Interface.Common;

[RegisterSingleton]
internal class RenderNotifier : IRenderNotifier
{
    private readonly ConcurrentDictionary<Guid, IRenderNotificationReceiver> observers = [];

    public IDisposable Subscribe(IRenderNotificationReceiver observer)
    {
        if(!observers.ContainsKey(observer.Id))
        {
            observers.TryAdd(observer.Id, observer);
        }
        return new Unsubscriber(observers, observer);
    }

    public async Task TriggerEvent(RenderNotification message) => Parallel.ForEach(observers, (o, CancellationToken) => o.Value.OnNext(message));

    public async Task Notify(string content)
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

public class RenderNotification
{
    public string Content { get; set; } = string.Empty;
}
public interface IRenderNotifier
{
    Task Notify(string content);
    IDisposable Subscribe(IRenderNotificationReceiver observer);
}

public interface IRenderNotificationReceiver : IObserver<RenderNotification>
{
    public Guid Id { get; }
}