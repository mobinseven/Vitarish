namespace Vitarish.Sea.Contracts.Responses;

public class DatabaseStatisticsReadDto
{
    [JsonPropertyName("projects")]
    public int Projects { get; set; }

    [JsonPropertyName("procedures")]
    public int Procedures { get; set; }

    [JsonPropertyName("events")]
    public int Events { get; set; }

    [JsonPropertyName("roles")]
    public int Roles { get; set; }

    [JsonPropertyName("attachments")]
    public int Attachments { get; set; }

    [JsonPropertyName("reminders")]
    public int Reminders { get; set; }
}

