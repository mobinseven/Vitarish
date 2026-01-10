namespace Vitarish.Interfaces.Base.Models;

/// <summary>
/// Configuration for the Back button.
/// </summary>
/// <param name="Visible">Whether the Back button should be visible.</param>
/// <param name="Uri">The URI to navigate to when clicked (if Action is not provided).</param>
/// <param name="Action">The custom action to execute when clicked (takes precedence over Uri).</param>
public sealed record BackTarget(
    bool Visible = true,
    string? Uri = null,
    Func<Task>? Action = null
);
