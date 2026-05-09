namespace Vitarish.Commerce.Domain.Dto;

public record CartReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public CartStatus Status { get; set; }
}
