namespace Vitarish.Commerce.Domain.Dto;

public record OrderReadDto : EntityReadDto
{
    public string MedusaId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public OrderStatus Status { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = default!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrderStats? Stats { get; set; }
}
