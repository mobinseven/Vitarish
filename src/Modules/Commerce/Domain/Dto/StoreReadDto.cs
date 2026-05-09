namespace Vitarish.Commerce.Domain.Dto;

public record StoreReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CurrencyCode { get; set; } = default!;
    public string DefaultRegionId { get; set; } = default!;
    public string MedusaBaseUrl { get; set; } = default!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StoreStats? Stats { get; set; }
}
