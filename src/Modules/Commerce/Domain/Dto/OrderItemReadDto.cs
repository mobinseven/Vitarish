using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record OrderItemReadDto : EntityReadDto
{
    public Guid OrderId { get; set; }
    public string VariantId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
