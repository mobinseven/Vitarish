namespace Vitarish.Sea.Domain.Dto;

public record DatabaseStatisticsReadDto(
    [property: JsonPropertyName("projects")] int Projects,
    [property: JsonPropertyName("procedures")] int Procedures,
    [property: JsonPropertyName("events")] int Events,
    [property: JsonPropertyName("roles")] int Roles,
    [property: JsonPropertyName("attachments")] int Attachments,
    [property: JsonPropertyName("reminders")] int Reminders);

