namespace Vitarish.Interfaces.Base.Models;

/// <summary>
/// Represents a single navigation button.
/// </summary>
/// <param name="Visible">Whether the button is visible.</param>
/// <param name="Enabled">Whether the button is enabled (clickable).</param>
/// <param name="OnClick">The action to execute when the button is clicked.</param>
public sealed record NavButton(
    bool Visible,
    bool Enabled,
    Func<Task> OnClick
);
