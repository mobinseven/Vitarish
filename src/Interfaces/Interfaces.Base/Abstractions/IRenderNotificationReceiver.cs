namespace Vitarish.Interfaces.Base.Abstractions;

public interface IRenderNotificationReceiver : IObserver<RenderNotification>
{
    Guid Id { get; }
}
