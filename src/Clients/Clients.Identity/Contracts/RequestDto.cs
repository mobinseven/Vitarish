namespace Vitarish.Clients.Identity.Contracts;

public class RequestDto
{

    public string Authority { get; set; }

    public string ClientId { get; set; }

    public string OwnerId { get; set; }

    public string? UserId { get; set; }

    public string? TenantId { get; set; }

    public string? ResourceId { get; set; }

    public string? RequestId { get; set; }
}
