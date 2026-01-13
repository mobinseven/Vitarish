using Vitarish.Sea.Interface.Plugins.Base.Enums;

namespace Vitarish.Sea.Interface.Plugins.Base.Projects;
/// <summary>
/// Provides metadata serialization helpers for project creation forms that operate without
/// relying on a specific runtime context. Implementations are expected to call
/// <see cref="InitializeEditableMetadata"/> during construction and respond to metadata
/// updates via <see cref="OnMetadataChanged"/> and <see cref="OnMetadataJsonChanged"/>.
/// </summary>
/// <typeparam name="TMetadata">The metadata contract emitted by the form.</typeparam>
/// <typeparam name="TEditableModel">The mutable editor model that powers the UI.</typeparam>
public abstract class ProjectCreationFormBase<TMetadata, TEditableModel> : ProjectsBase
    where TMetadata : class
    where TEditableModel : class
{


    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private bool editableMetadataInitialized;

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
        if (editableMetadata is null)
        {
            throw new ArgumentNullException(nameof(editableMetadata));
        }

        if (editableMetadataInitialized)
        {
            throw new InvalidOperationException("Editable metadata has already been initialized.");
        }

        EditableMetadata = editableMetadata;
        editableMetadataInitialized = true;
    }

    /// <summary>
    /// Serializes the supplied metadata, updates <see cref="Metadata"/> and
    /// <see cref="MetadataJson"/>, and invokes <see cref="OnMetadataChanged"/>.
    /// </summary>
    /// <param name="metadata">The metadata instance.</param>
    /// <param name="markAsClean">Indicates whether the metadata should be considered clean.</param>
    protected void SetMetadata(TMetadata? metadata, bool markAsClean)
    {
        Metadata = metadata;
        OnMetadataChanged(metadata);

        string payload = metadata is null
            ? string.Empty
            : SerializeMetadata(metadata);

        SetMetadataJson(payload, markAsClean);
    }

    /// <summary>
    /// Clears metadata state and notifies derived classes about the change.
    /// </summary>
    protected void ClearMetadata() => SetMetadata(null, markAsClean: true);

    /// <summary>
    /// Invoked whenever <see cref="Metadata"/> changes.
    /// </summary>
    /// <param name="metadata">The new metadata value.</param>
    protected virtual void OnMetadataChanged(TMetadata? metadata)
    {
    }

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
    /// <param name="markAsClean">Whether the JSON should be marked as clean.</param>
    protected void SetMetadataJson(string? value, bool markAsClean)
    {
        SuppressMetadataChangeNotification = true;
        MetadataJson = value ?? string.Empty;
        SuppressMetadataChangeNotification = false;

        OnMetadataJsonChanged(markAsClean);
    }

    /// <summary>
    /// Called whenever <see cref="MetadataJson"/> changes.
    /// </summary>
    /// <param name="markAsClean">Indicates whether the payload should be treated as clean.</param>
    protected abstract void OnMetadataJsonChanged(bool markAsClean);

    /// <summary>
    /// Attempts to deserialize metadata from <paramref name="json"/>.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="metadata">When successful, receives the normalized metadata.</param>
    /// <param name="error">Receives a localized error message when parsing fails.</param>
    /// <returns><see langword="true"/> when the metadata could be parsed.</returns>
    protected bool TryDeserializeMetadata(string json, out TMetadata? metadata, out string? error)
    {
        metadata = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = BuildEmptyMetadataErrorMessage();
            return false;
        }

        try
        {
            TMetadata? parsed = JsonSerializer.Deserialize<TMetadata>(json, serializerOptions);
            if (parsed is null)
            {
                error = BuildEmptyMetadataErrorMessage();
                return false;
            }

            metadata = NormalizeMetadata(parsed);
            return true;
        }
        catch (Exception ex)
        {
            error = BuildDeserializationErrorMessage(ex);
            return false;
        }
    }

    /// <summary>
    /// Builds the localized message used when the metadata payload is empty.
    /// </summary>
    protected abstract string BuildEmptyMetadataErrorMessage();

    /// <summary>
    /// Builds a localized message describing why deserialization failed.
    /// </summary>
    /// <param name="exception">The underlying exception.</param>
    protected abstract string BuildDeserializationErrorMessage(Exception exception);

    /// <summary>
    /// Normalizes the metadata after successful deserialization.
    /// </summary>
    /// <param name="metadata">The parsed metadata.</param>
    /// <returns>The normalized metadata.</returns>
    protected abstract TMetadata NormalizeMetadata(TMetadata metadata);

    /// <summary>
    /// Normalizes string input by trimming whitespace and converting empty strings to null.
    /// </summary>
    /// <param name="value">The raw input.</param>
    /// <returns>The normalized value.</returns>
    protected static string? NormalizeInput(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    protected string? FormatGender(Gender? gender) => gender switch
    {
        Gender.Male => Localizer["Male"],
        Gender.Female => Localizer["Female"],
        _ => Localizer["Unspecified"]
    };
}
