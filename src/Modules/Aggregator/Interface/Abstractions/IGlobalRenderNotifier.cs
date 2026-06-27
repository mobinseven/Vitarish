namespace Vitarish.Modules.Aggregator.Interface.Abstractions;

public interface IGlobalRenderNotifier
{
    event EventHandler? RenderRequired;
    Task Notify(EventArgs? content = null);
}
