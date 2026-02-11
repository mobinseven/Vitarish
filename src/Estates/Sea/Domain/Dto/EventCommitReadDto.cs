namespace Vitarish.Sea.Domain.Dto;

public record EventCommitReadDto(
    [property: JsonPropertyName("reporterUserId")] Guid ReporterUserId,
    [property: JsonPropertyName("commitTime")] DateTimeOffset CommitTime);
