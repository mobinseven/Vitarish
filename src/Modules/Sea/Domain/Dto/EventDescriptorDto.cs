namespace Vitarish.Sea.Domain.Dto;

public record EventDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("procedureId")]
    public Guid? ProcedureId { get; init; }

    [JsonPropertyName("previousEventId")]
    public Guid? PreviousEventId { get; init; }

    [JsonPropertyName("committedBy")]
    public Guid? CommittedBy { get; init; }

    [JsonPropertyName("committedFrom")]
    public DateTime? CommittedFrom { get; init; }

    [JsonPropertyName("committedTo")]
    public DateTime? CommittedTo { get; init; }

    [JsonPropertyName("isCommitted")]
    public bool? IsCommitted { get; init; }

    [JsonPropertyName("readRoleId")]
    public Guid? ReadRoleId { get; init; }

    [JsonPropertyName("writeRoleId")]
    public Guid? WriteRoleId { get; init; }

    //[JsonPropertyName("includeStats")]
    //public bool IncludeStats { get; set; } = true;
}

