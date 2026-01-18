using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;

public interface IRenderNotificationReceiver : IObserver<RenderNotification>
{
    Guid Id { get; }
}
