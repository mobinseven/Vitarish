using System.ComponentModel.DataAnnotations;

namespace Vitarish.Users.Domain.Entities;

public class Access
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
