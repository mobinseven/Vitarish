namespace Vitarish.Commerce.Domain.Dto;

public record CartWriteDto : EntityWriteDto
{
    public string? MedusaId { get; set; }
    public string? CustomerId { get; set; }
    public CartStatus? Status { get; set; }
}
