namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

public interface IRenderNotificationReceiver : IObserver<RenderNotification>
{
    Guid Id { get; }
}
