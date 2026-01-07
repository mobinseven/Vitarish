namespace Vitarish.Interfaces.Base.Services;

/// <summary>
/// Represents a configured navigation menu action.
/// </summary>
public sealed record NavMenuAction(string Icon, string? Href = null, Func<Task>? OnClick = null, string? AriaLabel = null)
{
    public Task InvokeAsync(NavigationManager navigationManager)
    {
        if (OnClick is not null)
        {
            return OnClick();
        }

        if (!string.IsNullOrWhiteSpace(Href))
        {
            navigationManager.NavigateTo(Href);
        }

        return Task.CompletedTask;
    }
}
