using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Abstractions;

public interface IRenderNotifier
{
    event EventHandler RenderRequired;
    Task Notify(EventArgs? args = null);
}
