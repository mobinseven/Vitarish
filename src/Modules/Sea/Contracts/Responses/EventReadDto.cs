namespace Vitarish.Sea.Contracts.Responses;

public record EventReadDto : EntityReadDto
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }

    [JsonPropertyName("procedureId")]
    public Guid ProcedureId { get; set; }

    [JsonPropertyName("previousEventId")]
    public Guid? PreviousEventId { get; set; }

    [JsonPropertyName("committedAt")]
    public DateTime? CommittedAt { get; set; }

    [JsonPropertyName("committedBy")]
    public Guid CommittedBy { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;


    [JsonPropertyName("isCommitted")]
    public bool IsCommitted { get; set; }

    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }
}

