using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;

public interface IRenderNotifier
{
    event EventHandler<RenderNotification> RenderRequired;
    Task Notify(object? content = null);
}
