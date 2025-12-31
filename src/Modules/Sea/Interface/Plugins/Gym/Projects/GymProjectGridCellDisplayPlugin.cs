namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;
/// <summary>
/// Displays gym trainee onboarding projects with a fitness-oriented summary in
/// the projects grid. Trainers can quickly review membership, program, and
/// contact details without opening the project dialog.
/// </summary>
public sealed class GymProjectGridCellDisplayPlugin : IProjectGridCellDisplayPlugin
{
    /// <summary>
    /// Identifier used within configuration metadata.
    /// </summary>
    public const string PluginId = "gym-project-grid-cell";

    /// <inheritdoc />
    public string Id => PluginId;

    /// <inheritdoc />
    public string DisplayName => "Gym project overview";

    /// <inheritdoc />
    public string? Description
        => "Surfaces trainee demographics, membership, and goals directly in the grid.";

    /// <inheritdoc />
    public bool IsAvailable(ProjectGridCellDisplayContext context) => GymProjectMetadataParser.HasGymMetadata(context.Project.Metadata);

    /// <inheritdoc />
    public RenderFragment Render(ProjectGridCellDisplayContext context) => builder =>
                                                                                    {
                                                                                        builder.OpenComponent<GymProjectGridCellDisplay>(0);
                                                                                        builder.AddAttribute(1, nameof(GymProjectGridCellDisplay.Context), context);
                                                                                        builder.CloseComponent();
                                                                                    };
}
