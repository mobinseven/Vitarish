using Injectio.Attributes;
using System.Runtime.CompilerServices;
using Vitarish.Modules.Base;

namespace Vitarish.Sea.Interface.Services;

[RegisterScoped]
internal class MetadataService(ITenantService tenantsService) : IMetadataService
{
    public string? PluginIdentifier
    {
        get => GetField<string>(); set => SetField(value);
    }

    public IEnumerable<UserDto>? Users => tenantsService.Users;

    private void SetField<T>(T value, [CallerMemberName] string propertyName = null!) => tenantsService.Metadata?[propertyName] = value ?? default!;
    private T? GetField<T>([CallerMemberName] string propertyName = null!)
    {
        if (tenantsService.Metadata?[propertyName] is JsonElement jsonElement)
        {
            T? res = JsonSerializer.Deserialize<T>(jsonElement);
            return res;
        }
        else
        {
            return tenantsService.Metadata?[propertyName] is T sameType ? sameType : default;
        }
    }
}

public interface IMetadataService
{
    IEnumerable<UserDto>? Users { get; }
    string? PluginIdentifier { get; set; }
}
