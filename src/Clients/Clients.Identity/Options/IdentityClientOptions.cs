namespace Vitarish.Clients.Identity.Options;

public class IdentityClientOptions
{
    public required string Authority { get; set; }
    public required string Secret { get; set; }
    public required string ClientId { get; set; }
    public bool? Local { get; set; }
    public bool? Dev { get; set; }
    public string? DataProtectionDomain { get; set; }
    public string? DataProtectionPassword { get; set; }
    public int CookieExpirationDays { get; set; } = 14;
}
