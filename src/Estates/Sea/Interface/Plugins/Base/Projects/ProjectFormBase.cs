namespace Vitarish.Sea.Interface.Plugins.Base.Projects;
/// <summary>
/// Provides metadata serialization helpers for project creation forms that operate without
/// relying on a specific runtime context. Implementations are expected to call
/// <see cref="InitializeEditableMetadata"/> during construction and respond to metadata
/// updates via <see cref="OnMetadataChanged"/> and <see cref="OnMetadataJsonChanged"/>.
/// </summary>
/// <typeparam name="TMetadata">The metadata contract emitted by the form.</typeparam>
/// <typeparam name="TEditableModel">The mutable editor model that powers the UI.</typeparam>
public abstract class ProjectFormBase<TMetadata, TEditableModel> : ProjectEditForm
    where TMetadata : class
    where TEditableModel : class
{


    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the editable model backing the form.
    /// </summary>
    protected TEditableModel EditableMetadata { get; private set; } = default!;

    /// <summary>
    /// Gets the normalized metadata currently associated with the form.
    /// </summary>
    protected TMetadata? Metadata { get; private set; }

    /// <summary>
    /// Gets the JSON payload representing <see cref="Metadata"/>.
    /// </summary>
    protected string MetadataJson { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether metadata change notifications should be suppressed.
    /// </summary>
    protected bool SuppressMetadataChangeNotification { get; private set; }

    /// <summary>
    /// Ensures <see cref="EditableMetadata"/> is available to derived classes.
    /// </summary>
    /// <remarks>
    /// The method can only be invoked once.
    /// </remarks>
    /// <param name="editableMetadata">The editor model instance.</param>
    protected void InitializeEditableMetadata(TEditableModel editableMetadata)
    {
        ArgumentNullException.ThrowIfNull(editableMetadata);

        EditableMetadata = editableMetadata;
    }

    /// <summary>
    /// Serializes the supplied metadata, updates <see cref="Metadata"/> and
    /// <see cref="MetadataJson"/>, and invokes <see cref="OnMetadataChanged"/>.
    /// </summary>
    /// <param name="metadata">The metadata instance.</param>
    protected void SetMetadata(TMetadata? metadata)
    {
        Metadata = metadata;
        string payload = metadata is null
            ? string.Empty
            : SerializeMetadata(metadata);

        SetMetadataJson(payload);
    }

    /// <summary>
    /// Clears metadata state and notifies derived classes about the change.
    /// </summary>
    protected void ClearMetadata() => SetMetadata(null);

    /// <summary>
    /// Serializes <paramref name="metadata"/> to JSON using <see cref="serializerOptions"/>.
    /// </summary>
    /// <param name="metadata">The metadata instance.</param>
    /// <returns>The serialized JSON payload.</returns>
    protected string SerializeMetadata(TMetadata metadata) => JsonSerializer.Serialize(metadata, serializerOptions);

    /// <summary>
    /// Updates the JSON payload and notifies derived classes.
    /// </summary>
    /// <param name="value">The new JSON payload.</param>
    protected void SetMetadataJson(string? value)
    {
        SuppressMetadataChangeNotification = true;
        MetadataJson = value ?? string.Empty;
        SuppressMetadataChangeNotification = false;
    }
}
