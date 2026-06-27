using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CustomerProfileWriteDto : EntityWriteDto
{
    public string? UserId { get; set; }
    public string? MedusaCustomerId { get; set; }
}
