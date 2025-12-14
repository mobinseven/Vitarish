namespace Vitarish.Clients.Modules.Sea.Dto;

public record ProcedureDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("roleIds")]
    public IReadOnlyList<Guid>? RoleIds { get; init; }

    //[JsonPropertyName("includeStats")]
    //public bool IncludeStats { get; set; } = true;
}

