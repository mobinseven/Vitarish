using System.Text.Json;

namespace Vitarish.Sea.Api;

public static class SeaContractApplicationMapper
{
    public static T Convert<T>(object value)
    {
        string json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException($"Could not map Sea contract/application value to {typeof(T).Name}.");
    }
}
