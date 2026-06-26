namespace Vitarish.Sea.Contracts.Responses;

public record ProcedureDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("roleIds")]
    public IReadOnlyList<Guid>? RoleIds { get; set; }

    //[JsonPropertyName("includeStats")]
    //public bool IncludeStats { get; set; } = true;
}

