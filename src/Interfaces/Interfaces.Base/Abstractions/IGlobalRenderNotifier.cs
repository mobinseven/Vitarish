namespace Vitarish.Interfaces.Base.Abstractions;

public interface IGlobalRenderNotifier
{
    event EventHandler? RenderRequired;
    Task Notify(object? content = null);
}
