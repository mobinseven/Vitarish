namespace Vitarish.Interfaces.Base.Common;

public record PluginContext(
    IReadOnlyDictionary<string, object?> Metadata = default)
{

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

public interface Plugin<T>
    where T : PluginContext
{
    string Id { get; }
    RenderFragment Render(T context);
}
