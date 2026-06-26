namespace Vitarish.Sea.Contracts.Responses;

public class EventCommitReadDto
{
    [JsonPropertyName("reporterUserId")]
    public Guid ReporterUserId { get; set; }

    [JsonPropertyName("commitTime")]
    public DateTime CommitTime { get; set; }
}
