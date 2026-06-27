using System.Text.Json.Serialization;

namespace Vitarish.Modules.Base.Interface.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypographySize
{
    Compact,
    Normal,
    Large,
    ExtraLarge
}
