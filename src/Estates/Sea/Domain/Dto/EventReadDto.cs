namespace Vitarish.Sea.Domain.Dto;

public record EventReadDto : EntityDto
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }

    [JsonPropertyName("procedureId")]
    public Guid ProcedureId { get; set; }

    [JsonPropertyName("previousEventId")]
    public Guid? PreviousEventId { get; set; }

    [JsonPropertyName("committedAt")]
    public DateTimeOffset? CommittedAt { get; set; }

    [JsonPropertyName("committedBy")]
    public Guid CommittedBy { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url => $"/sea/events/{Id}";

    [JsonPropertyName("isCommitted")]
    public bool IsCommitted => CommittedBy != default;

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }
}

