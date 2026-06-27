namespace Vitarish.Modules.Base.Interface.Abstractions;

public interface IRenderNotifier
{
    event EventHandler RenderRequired;
    Task Notify(EventArgs? args = null);
}
