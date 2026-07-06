using System.Text.Json;

namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
    private static readonly JsonSerializerOptions MappingJsonOptions = new(JsonSerializerDefaults.Web);

    public static T Convert<T>(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is T typedValue)
        {
            return typedValue;
        }

        // Central DTO-to-DTO adapter for same-shaped Sea application and contract models.
        object? mappedValue = JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(value, value.GetType(), MappingJsonOptions),
            MappingJsonOptions);

        return mappedValue is null
            ? throw new NotSupportedException($"Mapping for {value.GetType().Name} to {typeof(T).Name} produced a null value.")
            : (T)mappedValue;
    }
}
