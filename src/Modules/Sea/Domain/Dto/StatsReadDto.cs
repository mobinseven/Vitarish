namespace Vitarish.Sea.Domain.Dto;

public sealed class StatsReadDto
{
    [JsonPropertyName("state")]
    public ProgressState? State { get; set; }

    [JsonPropertyName("procedures")]
    public int Procedures { get; set; }

    [JsonPropertyName("events")]
    public int Events { get; set; }

    [JsonPropertyName("committedEvents")]
    public int CommittedEvents { get; set; }

    [JsonPropertyName("eventItems")]
    public int EventItems { get; set; }

    [JsonPropertyName("newItems")]
    public int NewItems { get; set; }

    [JsonPropertyName("unread")]
    public bool Unread { get; set; }

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; }
}

