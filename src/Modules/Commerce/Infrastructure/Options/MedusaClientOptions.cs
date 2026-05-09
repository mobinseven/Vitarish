namespace Vitarish.Commerce.Infrastructure.Options;

public class MedusaClientOptions
{
    public const string SectionName = "Vitarish:Commerce";

    public string MedusaBaseUrl { get; set; } = string.Empty;
    public string AdminApiToken { get; set; } = string.Empty;
    public string StorefrontPublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
