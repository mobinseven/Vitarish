namespace Vitarish.Sea.Contracts.Responses;

public record UserRoleReadDto : EntityReadDto
{
    [JsonPropertyName("roleId")]
    public Guid RoleId { get; set; }

    [JsonPropertyName("type")]
    public RoleType Type { get; set; }
}
