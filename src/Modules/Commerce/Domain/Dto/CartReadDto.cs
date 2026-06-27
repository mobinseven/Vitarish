using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record CartReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public CartStatus Status { get; set; }
    public CartRecoveryStatus RecoveryStatus { get; set; }
    public string RecoveryNote { get; set; } = string.Empty;
}
