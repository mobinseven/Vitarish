using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
internal class RenderNotifier : IRenderNotifier, IDisposable
{
    public event EventHandler<RenderNotification>? RenderRequired;

    private readonly IGlobalRenderNotifier globalRenderNotifier;
    public RenderNotifier(IGlobalRenderNotifier globalRenderNotifier)
    {
        this.globalRenderNotifier = globalRenderNotifier;
        globalRenderNotifier.RenderRequired += Notify;
    }
    public void Dispose() => globalRenderNotifier.RenderRequired -= Notify;
    public void Notify(object? sender, EventArgs message) => RenderRequired?.Invoke(sender, new()
    {
        Content = message,
    });
    public async Task Notify(object? content = null)
    {
        RenderNotification renderNotification = new()
        {
            Content = content,
        };

        Notify(this, renderNotification);
    }
}
