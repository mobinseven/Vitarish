namespace Vitarish.Clients.Interface.Identity.Contracts;

public class AccessDto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ClientId { get; set; } = default!;

    public string OwnerId { get; set; } = default!;

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public string? ResourceId { get; set; }
}
