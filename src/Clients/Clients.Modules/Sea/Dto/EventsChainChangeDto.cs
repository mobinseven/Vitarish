namespace Vitarish.Clients.Modules.Sea.Dto;

public record EventChainChangeDto(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("previousEventId")] Guid? PreviousEventId);

