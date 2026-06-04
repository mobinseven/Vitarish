namespace Vitarish.Commerce.Domain.Models;

public sealed class StoreStats
{
    public Guid StoreId { get; init; }
    public int TotalProducts { get; init; }
    public int PublishedProducts { get; init; }
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public DateTime? LastOrderAt { get; init; }
}
