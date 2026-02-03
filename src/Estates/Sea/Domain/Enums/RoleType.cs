namespace Vitarish.Sea.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<RoleType>))]
public enum RoleType
{
    Read,
    Write
}
