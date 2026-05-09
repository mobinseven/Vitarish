namespace Vitarish.Commerce.Domain.Dto;

public record StoreWriteDto : EntityWriteDto
{
    public string? MedusaId { get; set; }
    public string? Name { get; set; }
    public string? CurrencyCode { get; set; }
    public string? DefaultRegionId { get; set; }
    public string? MedusaBaseUrl { get; set; }
    public string? AdminApiToken { get; set; }
    public string? StorefrontPublishableKey { get; set; }
}
