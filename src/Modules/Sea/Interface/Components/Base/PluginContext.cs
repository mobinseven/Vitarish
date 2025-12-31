namespace Vitarish.Sea.Interface.Components.Base;
/// <summary>
/// Carries runtime information about a project creation request when invoking
/// <see cref="IProjectCreationFormPlugin"/> instances.
/// </summary>
/// <param name="Services">The scoped service provider resolved for the dialog lifetime.</param>
/// <param name="TemplateId">The optional template requested by the caller.</param>
/// <param name="Metadata">Additional values describing the request, such as a parent entity identifier.</param>
public record PluginContext(
    IServiceProvider Services,
    IReadOnlyDictionary<string, object?> Metadata)
{
    /// <summary>
    /// Provides quick access to scoped services that plugins frequently need, such as
    /// localization, security, or logging services.
    /// </summary>
    /// <typeparam name="T">The requested service type.</typeparam>
    public T GetRequiredService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    /// <summary>
    /// Attempts to retrieve a metadata item that was supplied when the creation flow
    /// was initiated.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">When the key exists and the type matches, receives the stored value.</param>
    /// <returns><see langword="true"/> when the value was found.</returns>
    public bool TryGetMetadata<T>(string key, out T? value)
    {
        if (Metadata.TryGetValue(key, out object? raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}
