using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CartItemWriteDto : EntityWriteDto
{
    public Guid? CartId { get; set; }
    public string? VariantId { get; set; }
    public string? Title { get; set; }
    public int? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
