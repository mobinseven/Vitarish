namespace Vitarish.Sea.Interface.Plugins.Base.Projects;
/// <summary>
/// Provides display helpers for project grid cell components so they can render
/// metadata-driven summaries without depending on a specific <see cref="ProjectGridCellDisplayContext"/>.
/// </summary>
/// <typeparam name="TMetadata">The metadata contract associated with the project.</typeparam>
public abstract class ProjectGridCellDisplayBase<TMetadata> : ProjectsBase
    where TMetadata : class
{
    private readonly string? initials;
    private readonly string? demographicsLine;
    private readonly string? contactLine;

    /// <summary>
    /// Gets the parsed metadata associated with the project.
    /// </summary>
    protected TMetadata? Metadata { get; private set; }

    /// <summary>
    /// Gets the display name resolved from the metadata.
    /// </summary>
    protected string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the chip label derived from the metadata.
    /// </summary>
    protected string? ChipLabel { get; private set; }

    /// <summary>
    /// Updates the cached display state using the supplied metadata payload.
    /// </summary>
    /// <param name="metadataJson">The project metadata payload.</param>
    /// <param name="projectName">The fallback project name.</param>
    protected void UpdateDisplayState(string? metadataJson, string projectName)
    {
        Metadata = ParseMetadata(metadataJson);
        OnMetadataAssigned(metadataJson);

        ChipLabel = BuildChipLabel();
        DisplayName = BuildDisplayName(projectName);

        OnStateUpdated();
    }

    /// <summary>
    /// Invoked after <see cref="Metadata"/> is assigned.
    /// </summary>
    /// <param name="metadataJson">The raw metadata payload.</param>
    protected virtual void OnMetadataAssigned(string? metadataJson)
    {
    }

    /// <summary>
    /// Invoked after the derived display state has been computed.
    /// </summary>
    protected virtual void OnStateUpdated()
    {
    }

    /// <summary>
    /// Parses the metadata payload.
    /// </summary>
    /// <param name="metadataJson">The raw metadata payload.</param>
    /// <returns>The parsed metadata instance.</returns>
    protected abstract TMetadata? ParseMetadata(string? metadataJson);

    /// <summary>
    /// Retrieves the default display name to use when no name information is available.
    /// </summary>
    protected abstract string GetDefaultDisplayName();

    /// <summary>
    /// Retrieves the chip label rendered alongside the project, if any.
    /// </summary>
    /// <param name="metadata">The metadata instance.</param>
    protected virtual string? GetChipLabel(TMetadata metadata) => null;

    /// <summary>
    /// Invokes the supplied callback if it has been configured.
    /// </summary>
    /// <param name="onClicked">The callback associated with the grid cell.</param>
    /// <param name="projectId">The identifier of the project.</param>
    protected static async Task InvokeClickAsync(EventCallback<Guid> onClicked, Guid projectId)
    {
        if (!onClicked.HasDelegate)
        {
            return;
        }

        await onClicked.InvokeAsync(projectId);
    }

    /// <summary>
    /// Formats a value while substituting missing entries with a localized placeholder.
    /// </summary>
    /// <param name="value">The value to format.</param>
    protected string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? Localizer["NotProvided"] : value!;

    /// <summary>
    /// Formats a gender value using localized resources.
    /// </summary>
    /// <param name="gender">The gender value.</param>
    protected string? FormatGender(string? gender) => string.IsNullOrWhiteSpace(gender) ? (string?)null : Localizer[gender!];

    /// <summary>
    /// Formats a date value using the current culture.
    /// </summary>
    /// <param name="value">The ISO-8601 date value.</param>
    protected string? FormatDate(string? value) => string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed.ToString("d", CultureInfo.CurrentCulture)
            : value;

    protected virtual string BuildDisplayName(string projectName) => projectName;

    protected virtual string? BuildChipLabel() => null;
}
