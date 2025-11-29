namespace Vorn.Interface.Services;

using System;
using Injectio.Attributes;

[RegisterScoped]
internal sealed class LayoutService : ILayoutService
{
    public event EventHandler? ThemeStateChanged;

    public event EventHandler? SearchStateChanged;

    public void NotifyThemeStateChanged(object? sender = null)
        => ThemeStateChanged?.Invoke(sender ?? this, EventArgs.Empty);

    public void NotifySearchStateChanged(object? sender = null)
        => SearchStateChanged?.Invoke(sender ?? this, EventArgs.Empty);
}

public interface ILayoutService
{
    event EventHandler? ThemeStateChanged;

    event EventHandler? SearchStateChanged;

    void NotifyThemeStateChanged(object? sender = null);

    void NotifySearchStateChanged(object? sender = null);
}
