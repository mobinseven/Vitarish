namespace Vitarish.Sea.Domain.Dto;

public record ProcedureWriteDto(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("metadata")] string Metadata = "{}") : EntityWriteDto;
