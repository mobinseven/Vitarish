namespace Vitarish.Sea.Interface.Components.Projects.Plugins;
/// <summary>
/// Provides the built-in project grid cell presentation.
/// </summary>
public sealed class DefaultProjectGridCellDisplayPlugin : IProjectGridCellDisplayPlugin
{
    public const string PluginId = "default-project-grid-cell";

    /// <inheritdoc />
    public string Id => PluginId;

    /// <inheritdoc />
    public string DisplayName => "Default project cell";

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public bool IsAvailable(ProjectGridCellDisplayContext context) => true;

    /// <inheritdoc />
    public RenderFragment Render(ProjectGridCellDisplayContext context)
        => builder =>
        {
            builder.OpenComponent<DefaultProjectGridCellDisplay>(0);
            builder.AddAttribute(1, nameof(DefaultProjectGridCellDisplay.Context), context);
            builder.CloseComponent();
        };
}
