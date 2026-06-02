namespace Vitarish.Interfaces.Base.Services;

[RegisterSingleton]
internal class GlobalRenderNotifier : IGlobalRenderNotifier
{
    public event EventHandler? RenderRequired;

    public async Task Notify(EventArgs? content = null) => RenderRequired?.Invoke(this, content);
}
