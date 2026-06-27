using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record ProductVariantReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public Guid ProductId { get; set; }
    public string Title { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public int InventoryQuantity { get; set; }
}
