namespace Vitarish.Clients.Modules.Sea.Dto;

public record EventDto(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("procedureId")] Guid ProcedureId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("previousEventId")] Guid? PreviousEventId,
    [property: JsonPropertyName("committedAt")] DateTimeOffset? CommittedAt,
    [property: JsonPropertyName("committedBy")] Guid CommittedBy) : EntityDto
{
    [JsonPropertyName("url")]
    public string Url => $"/sea/events/{Id}";

    [JsonPropertyName("isCommitted")]
    public bool IsCommitted => CommittedBy != default;

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsDto? Stats { get; init; }
}

