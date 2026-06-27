using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record ProductWriteDto : EntityWriteDto
{
    public string? MedusaId { get; set; }
    public string? Handle { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ProductStatus? Status { get; set; }
}
