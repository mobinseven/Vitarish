using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record UserRoleDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("userId")]
    public Guid? UserId { get; init; }

    [JsonPropertyName("roleId")]
    public Guid? RoleId { get; init; }

    [JsonPropertyName("type")]
    public RoleType? Type { get; init; }
}

