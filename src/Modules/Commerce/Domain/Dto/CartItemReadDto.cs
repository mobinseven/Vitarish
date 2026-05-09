namespace Vitarish.Commerce.Domain.Dto;

public record CartItemReadDto : EntityReadDto
{
    public Guid CartId { get; set; }
    public string VariantId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
