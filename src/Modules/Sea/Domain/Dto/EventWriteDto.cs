namespace Vitarish.Sea.Domain.Dto;

public record EventWriteDto(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("procedureId")] Guid ProcedureId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("previousEventId")] Guid? PreviousEventId,
    [property: JsonPropertyName("committedAt")] DateTimeOffset? CommittedAt,
    [property: JsonPropertyName("committedBy")] Guid CommittedBy) : EntityWriteDto;
