namespace Vitarish.Clients.Interface.Sea.Plugins.Clinic.Projects;
/// <summary>
/// Presents clinic intake projects with patient-focused metadata inside the
/// projects grid. The plugin surfaces demographic and visit information so
/// care teams can review context without opening the full project.
/// </summary>
public sealed class ClinicProjectGridCellDisplayPlugin : IProjectGridCellDisplayPlugin
{
    /// <summary>
    /// The identifier surfaced to configuration screens.
    /// </summary>
    public const string PluginId = "clinic-project-grid-cell";

    /// <inheritdoc />
    public string Id => PluginId;

    /// <inheritdoc />
    public string DisplayName => "Clinic project overview";

    /// <inheritdoc />
    public string? Description
        => "Summarises patient intake and insurance details for clinic projects.";

    /// <inheritdoc />
    public bool IsAvailable(ProjectGridCellDisplayContext context) => ClinicProjectMetadataParser.HasClinicMetadata(context.Project.Metadata);

    /// <inheritdoc />
    public RenderFragment Render(ProjectGridCellDisplayContext context) => builder =>
                                                                                    {
                                                                                        builder.OpenComponent<ClinicProjectGridCellDisplay>(0);
                                                                                        builder.AddAttribute(1, nameof(ClinicProjectGridCellDisplay.Context), context);
                                                                                        builder.CloseComponent();
                                                                                    };
}
