namespace Vitarish.Sea.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ProgressState>))]
public enum ProgressState
{
    NotStarted,
    Quarter,
    Half,
    ThreeQuarters,
    InProgress,
    Completed
}
