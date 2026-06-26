namespace Vitarish.Sea.Contracts.Responses;

public record RoleWriteDto(
    [property: JsonPropertyName("name")] string Name) : EntityWriteDto;
