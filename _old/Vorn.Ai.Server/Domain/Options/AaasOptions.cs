namespace Vorn.Ai.Server.Domain.Options
{
    public class AaasOptions
    {
        public string Authority { get; set; }
        public string Secret { get; set; }
        public string ClientId { get; set; }
        public object? Local { get; set; }
        public string? DataProtectionDomain { get; set; }
        public string? DataProtectionPassword { get; set; }
        public int CookieExpirationDays { get; set; } = 14;
    }
}
