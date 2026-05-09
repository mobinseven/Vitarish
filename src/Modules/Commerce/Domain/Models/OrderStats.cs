namespace Vitarish.Commerce.Domain.Models;

public sealed class OrderStats
{
    public Guid OrderId { get; init; }
    public int ItemCount { get; init; }
    public decimal TotalAmount { get; init; }
}
