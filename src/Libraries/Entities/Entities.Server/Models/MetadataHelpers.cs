using System.Runtime.CompilerServices;
public static class MetadataHelpers
{

    extension(Metadata Metadata)
    {
        public T? GetField<T>(
        Dictionary<string, object?> source,
        [CallerMemberName] string propertyName = null!)
        {
            if (source.Count != Metadata.Count)
            {
                Metadata = new(source);
            }

            string pascalKey = ToPascal(propertyName);
            string camelKey = JsonNamingPolicy.CamelCase.ConvertName(pascalKey);

            if (!Metadata.TryGetValue(pascalKey, out object? raw))
            {
                if (Metadata.TryGetValue(camelKey, out raw))
                {
                    if (!Metadata.ContainsKey(pascalKey))
                    {
                        Metadata[pascalKey] = raw;
                    }

                    _ = Metadata.Remove(camelKey);
                }
                else
                {
                    return default;
                }
            }

            return raw is null ? default : raw is JsonElement je ? je.Deserialize<T>() : raw is T t ? t : default;
        }

        private static string ToPascal(string name)
            => string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name[1..];

        public void SetField<T>(T value, [CallerMemberName] string propertyName = null!) => Metadata[propertyName] = value ?? default!;
    }
}
