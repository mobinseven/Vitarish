namespace Vitarish.Sea.Domain.Dto;

public sealed record StatsReadDto
{
    [property: JsonPropertyName("state")] public ProgressState? State { get; set; }
    [property: JsonPropertyName("procedures")] public int Procedures { get; set; }
    [property: JsonPropertyName("events")] public int Events { get; set; }
    [property: JsonPropertyName("committedEvents")] public int CommittedEvents { get; set; }
    [property: JsonPropertyName("eventItems")] public int EventItems { get; set; }
    [property: JsonPropertyName("newItems")] public int NewItems { get; set; }
    [property: JsonPropertyName("unread")] public bool Unread { get; set; }
    [property: JsonPropertyName("lastUpdate")] public DateTimeOffset? LastUpdate { get; set; }
}

