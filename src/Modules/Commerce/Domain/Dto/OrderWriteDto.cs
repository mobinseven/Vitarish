namespace Vitarish.Commerce.Domain.Dto;

public record OrderWriteDto : EntityWriteDto
{
    public string? MedusaId { get; set; }
    public string? CustomerId { get; set; }
    public OrderStatus? Status { get; set; }
    public FulfillmentStatus? FulfillmentStatus { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? CurrencyCode { get; set; }
}
