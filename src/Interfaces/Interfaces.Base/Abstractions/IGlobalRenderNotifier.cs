namespace Vitarish.Interfaces.Base.Abstractions;

public interface IGlobalRenderNotifier
{
    event EventHandler? RenderRequired;
    Task Notify(EventArgs? content = null);
}
