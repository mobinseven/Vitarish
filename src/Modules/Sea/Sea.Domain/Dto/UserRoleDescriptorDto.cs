namespace Vitarish.Sea.Domain.Dto;

public record UserRoleDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("userId")]
    public Guid? UserId { get; init; }

    [JsonPropertyName("roleId")]
    public Guid? RoleId { get; init; }

    [JsonPropertyName("type")]
    public RoleType? Type { get; init; }
}

