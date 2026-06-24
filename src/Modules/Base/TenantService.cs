using System.Runtime.CompilerServices;
using System.Text.Json;
using Vitarish.Entities.Contracts;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Modules.Base;

public class TenantService : ITenantService
{
    public Tenant? Tenant { get; private set; }
    public Metadata? Metadata { get; set; }
    public IEnumerable<UserDto> Users { get; set; } = [];
    public string? PluginIdentifier
    {
        get => GetField<string>(); set => SetField(value);
    }

    public void SetField<T>(T value, [CallerMemberName] string propertyName = null!) => Metadata?[propertyName] = value ?? default!;
    public T? GetField<T>([CallerMemberName] string propertyName = null!)
    {
        if (Metadata?[propertyName] is JsonElement jsonElement)
        {
            T? res = JsonSerializer.Deserialize<T>(jsonElement);
            return res;
        }
        else
        {
            return Metadata?[propertyName] is T sameType ? sameType : default;
        }
    }

}

public interface ITenantService
{
    string? PluginIdentifier { get; set; }
    Metadata? Metadata { get; set; }
    IEnumerable<UserDto> Users { get; set; }
    Tenant? Tenant { get; }

    T? GetField<T>([CallerMemberName] string? propertyName = null);
    void SetField<T>(T value, [CallerMemberName] string? propertyName = null);
}
