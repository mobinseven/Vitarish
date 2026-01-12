using System.Text.Json.Serialization;

namespace Vitarish.Interfaces.Base.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypographySize
{
    Compact,
    Normal,
    Large,
    ExtraLarge
}
