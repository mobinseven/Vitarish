namespace Vitarish.Interfaces.Base.Services;

[RegisterScoped]
internal sealed class LayoutService : ILayoutService
{
    public event EventHandler? SearchStateChanged;

    public void NotifySearchStateChanged(object? sender = null)
        => SearchStateChanged?.Invoke(sender ?? this, EventArgs.Empty);
}

public interface ILayoutService
{

    event EventHandler? SearchStateChanged;

    void NotifySearchStateChanged(object? sender = null);
}
