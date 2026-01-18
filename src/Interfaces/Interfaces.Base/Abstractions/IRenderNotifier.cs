namespace Vitarish.Interfaces.Base.Abstractions;

public interface IRenderNotifier
{
    Task Notify(object? content);
    IDisposable Subscribe(IRenderNotificationReceiver observer);
}
