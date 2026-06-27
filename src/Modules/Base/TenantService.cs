using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Vitarish.Modules.Base;

public sealed record TenantUserSummary(string Id, string? FullName)
{
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}

public sealed record TenantPresentationState(
    string? TenantId,
    bool IsTenantOwner,
    IReadOnlyList<TenantUserSummary> Users,
    IReadOnlyDictionary<string, object?> Metadata);

public interface ITenantPresentationContext
{
    TenantPresentationState State { get; }
}

public class TenantService : ITenantService, ITenantPresentationContext
{
    private Dictionary<string, object?> metadata = [];

    public TenantPresentationState State => new(TenantId, IsTenantOwner, Users.ToArray(), metadata);
    public string? TenantId { get; set; }
    public bool IsTenantOwner { get; set; }
    public IEnumerable<TenantUserSummary> Users { get; set; } = [];
    public IDictionary<string, object?> Metadata
    {
        get => metadata;
        set => metadata = new Dictionary<string, object?>(value ?? new Dictionary<string, object?>(), StringComparer.Ordinal);
    }

    public string? PluginIdentifier
    {
        get => GetField<string>();
        set => SetField(value);
    }

    public void SetField<T>(T value, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName is not null)
        {
            metadata[propertyName] = value;
        }
    }

    public T? GetField<T>([CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null || !metadata.TryGetValue(propertyName, out object? value) || value is null)
        {
            return default;
        }

        return value is JsonElement jsonElement
            ? jsonElement.Deserialize<T>()
            : value is T sameType ? sameType : default;
    }
}

public interface ITenantService
{
    string? PluginIdentifier { get; set; }
    IDictionary<string, object?> Metadata { get; set; }
    IEnumerable<TenantUserSummary> Users { get; set; }
    string? TenantId { get; set; }
    bool IsTenantOwner { get; set; }

    T? GetField<T>([CallerMemberName] string? propertyName = null);
    void SetField<T>(T value, [CallerMemberName] string? propertyName = null);
}
