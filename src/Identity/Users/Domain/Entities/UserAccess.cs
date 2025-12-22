using System.ComponentModel.DataAnnotations;

namespace Vitarish.Users.Domain.Entities;

public class UserAccess
{
    public object?[] Key => [ClientId, OwnerId, TenantId, UserId, ResourceId];
    [MaxLength(128)]
    public string ClientId { get; set; } = default!;

    [MaxLength(128)]
    public string OwnerId { get; set; } = default!;

    [MaxLength(256)]
    public string? TenantId { get; set; }

    [MaxLength(128)]
    public string? UserId { get; set; }

    [MaxLength(512)]
    public string? ResourceId { get; set; }
}
