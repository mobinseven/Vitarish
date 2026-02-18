using System.Runtime.CompilerServices;
using System.Text.Json;
public static class MetadataHelpers
{

    extension(Metadata Metadata)
    {
        public T? GetField<T>(Dictionary<string, object?> source, [CallerMemberName] string propertyName = null!)
        {
            if (source.Count != Metadata.Count)
            {
                Metadata = new(source);
            }
            T? val;
            if (Metadata[propertyName] is JsonElement jsonElement)
            {
                T? res = JsonSerializer.Deserialize<T>(jsonElement);
                val = res;
            }
            else
            {
                val = Metadata[propertyName] is T sameType ? sameType : default;
            }
            return val;
        }

        public void SetField<T>(T value, [CallerMemberName] string propertyName = null!) => Metadata[propertyName] = value ?? default!;
    }
}
