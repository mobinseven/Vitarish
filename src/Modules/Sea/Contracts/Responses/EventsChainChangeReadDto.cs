namespace Vitarish.Sea.Contracts.Responses;

public class EventsChainChangeReadDto
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("previousEventId")]
    public Guid? PreviousEventId { get; set; }
}

