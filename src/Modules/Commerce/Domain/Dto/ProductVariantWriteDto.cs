using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record ProductVariantWriteDto : EntityWriteDto
{
    public string? MedusaId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Title { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }
    public int? InventoryQuantity { get; set; }
}
