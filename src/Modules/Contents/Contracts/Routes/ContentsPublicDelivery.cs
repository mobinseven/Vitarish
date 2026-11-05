namespace Vitarish.Contents.Contracts.Routes;

public static class ContentsPublicDelivery
{
    public const string AnonymousJustification = "contents.published-content";
    public const string RateLimitPolicy = "contents.public-delivery";
    // Owned by TenantResolver.TenantHostHeaderName; kept here because Contracts cannot reference tenant infrastructure.
    public const string TenantHostHeader = "X-Vitarish-Tenant-Host";
}
