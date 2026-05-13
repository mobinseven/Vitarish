using System.Text.Json.Serialization;

namespace Vitarish.Interfaces.Base.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackCalendarViewMode
{
    Month,
    Week
}
