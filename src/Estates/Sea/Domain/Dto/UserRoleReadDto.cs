namespace Vitarish.Sea.Domain.Dto;

public record UserRoleReadDto : EntityDto
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("type")]
    public RoleType Type { get; set; }
}
