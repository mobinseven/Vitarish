namespace Vitarish.Clients.Modules.Sea.Dto;

public record EventRoleDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("eventId")]
    public Guid? EventId { get; init; }

    [JsonPropertyName("roleId")]
    public Guid? RoleId { get; init; }

    [JsonPropertyName("type")]
    public RoleType? Type { get; init; }
}
