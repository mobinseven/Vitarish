using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
internal class RenderNotifier : IRenderNotifier, IDisposable
{
    public event EventHandler? RenderRequired;

    private readonly IGlobalRenderNotifier globalRenderNotifier;
    public RenderNotifier(IGlobalRenderNotifier globalRenderNotifier)
    {
        this.globalRenderNotifier = globalRenderNotifier;
        globalRenderNotifier.RenderRequired += Notify;
    }
    public void Dispose() => globalRenderNotifier.RenderRequired -= Notify;
    public void Notify(object? sender, EventArgs message) => RenderRequired?.Invoke(sender, message);
    public async Task Notify(EventArgs? args = null) => Notify(this, args ?? new());
}
