namespace Vitarish.Sea.Domain.Dto;

public record EventReadDto(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("procedureId")] Guid ProcedureId,
    [property: JsonPropertyName("previousEventId")] Guid? PreviousEventId,
    [property: JsonPropertyName("committedAt")] DateTimeOffset? CommittedAt,
    [property: JsonPropertyName("committedBy")] Guid CommittedBy) : EntityDto
{
    [property: JsonPropertyName("title")]
    public string Title { get; set; }

    [property: JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("url")]
    public string Url => $"/sea/events/{Id}";

    [JsonPropertyName("isCommitted")]
    public bool IsCommitted => CommittedBy != default;

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; init; }
}

