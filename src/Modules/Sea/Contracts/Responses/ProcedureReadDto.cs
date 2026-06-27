using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record ProcedureReadDto : EntityReadDto
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;


    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StatsReadDto? Stats { get; set; }

    public string? Description { get; set; }
}

