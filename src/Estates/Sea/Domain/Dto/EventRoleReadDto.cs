namespace Vitarish.Sea.Domain.Dto;

public record EventRoleReadDto : EntityDto
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("type")]
    public RoleType Type { get; set; }
}
