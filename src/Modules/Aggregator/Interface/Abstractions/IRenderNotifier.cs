namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

public interface IRenderNotifier
{
    event EventHandler RenderRequired;
    Task Notify(EventArgs? args = null);
}
