using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CategoryReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Handle { get; set; } = default!;
    public Guid? ParentCategoryId { get; set; }
}
