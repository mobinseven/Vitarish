namespace Vitarish.Sea.Domain.Dto;

public record EventCommitDto(
    [property: JsonPropertyName("reporterUserId")] Guid ReporterUserId,
    [property: JsonPropertyName("commitTime")] DateTimeOffset CommitTime);
