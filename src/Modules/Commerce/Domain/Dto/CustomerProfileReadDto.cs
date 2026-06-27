using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CustomerProfileReadDto : EntityReadDto
{
    public string UserId { get; set; } = default!;
    public string MedusaCustomerId { get; set; } = default!;
}
