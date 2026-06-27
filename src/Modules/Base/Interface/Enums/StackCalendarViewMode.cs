using System.Text.Json.Serialization;

namespace Vitarish.Modules.Base.Interface.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackCalendarViewMode
{
    Month,
    Week
}
