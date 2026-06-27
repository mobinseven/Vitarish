using System.Text.Json.Serialization;

namespace Vitarish.Modules.Aggregator.Interface.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackCalendarViewMode
{
    Month,
    Week
}
