namespace Vitarish.Sea.Domain.Dto;

public class EventCommitReadDto
{
    [JsonPropertyName("reporterUserId")]
    public Guid ReporterUserId { get; set; }

    [JsonPropertyName("commitTime")]
    public DateTime CommitTime { get; set; }
}
