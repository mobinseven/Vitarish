using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record ProductReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public string Handle { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ProductStatus Status { get; set; }
    public string Url => $"/shop/products/{Handle}";
}
