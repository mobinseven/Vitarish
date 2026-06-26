namespace Vitarish.Sea.Contracts.Responses;

public record EventRoleReadDto : EntityReadDto
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("type")]
    public RoleType Type { get; set; }
}
