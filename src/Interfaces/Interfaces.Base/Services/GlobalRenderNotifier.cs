using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Services;

[RegisterSingleton]
internal class GlobalRenderNotifier : IGlobalRenderNotifier
{
    public event EventHandler? RenderRequired;

    public async Task Notify(object? content = null) => RenderRequired?.Invoke(this, new RenderNotification { Content = content });
}
