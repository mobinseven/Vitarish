namespace Vitarish.Sea.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<RoleType>))]
public enum RoleType
{
    Read,
    Write
}
