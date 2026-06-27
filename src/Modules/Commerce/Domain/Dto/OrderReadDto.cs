using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Commerce.Domain.Dto;

public record OrderReadDto : EntityReadDto
{
    public int OrderNumber { get; set; }
    public string MedusaId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string CustomerDisplay { get; set; } = "Guest";
    public OrderStatus Status { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public RefundStatus RefundStatus { get; set; }
    public string RefundNote { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrderStats? Stats { get; set; }
}
