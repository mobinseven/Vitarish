namespace Vitarish.Modules.Base.Interface.Abstractions;

public interface IRenderNotificationReceiver : IObserver<RenderNotification>
{
    Guid Id { get; }
}
