namespace Vitarish.Clients.Modules.Sea.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<RoleType>))]
public enum RoleType
{
    Read,
    Write
}
