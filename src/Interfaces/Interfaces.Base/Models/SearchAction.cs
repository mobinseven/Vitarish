namespace Vitarish.Interfaces.Base.Models;

/// <summary>
/// Configuration for the Search button.
/// </summary>
/// <param name="Visible">Whether the Search button is visible.</param>
/// <param name="Enabled">Whether the Search button is enabled.</param>
/// <param name="Uri">The URI to navigate to when clicked (if Action is not provided).</param>
/// <param name="Action">Custom action to execute when clicked (takes precedence over Uri).</param>
public sealed record SearchAction(
    bool Visible = true,
    bool Enabled = true,
    string? Uri = null,
    Func<Task>? Action = null
);
