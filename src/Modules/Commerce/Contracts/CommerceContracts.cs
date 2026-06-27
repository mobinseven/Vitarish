using Vitarish.Entities.Contracts;

namespace Vitarish.Commerce.Contracts;

public enum ProductStatus { Draft, Proposed, Published, Rejected }
public enum OrderStatus { Pending, Confirmed, Processing, Shipped, Delivered, Canceled, Refunded }
public enum FulfillmentStatus { NotFulfilled, PartiallyFulfilled, Fulfilled, PartiallyShipped, Shipped, PartiallyReturned, Returned, Canceled, RequiresAction }
public enum RefundStatus { None, Requested, Processing, Refunded, Rejected }
public enum CartRecoveryStatus { None, ContactPending, Contacted, Recovered, Ignored }

public record ProductDescriptorDto : EntityDescriptorDto { public ProductStatus? Status { get; set; } public Guid? CategoryId { get; set; } }
public record ProductReadDto : EntityReadDto { public string MedusaId { get; set; } = string.Empty; public string Handle { get; set; } = string.Empty; public string Title { get; set; } = string.Empty; public string? Description { get; set; } public ProductStatus Status { get; set; } public string Url { get; set; } = string.Empty; }
public record OrderDescriptorDto : EntityDescriptorDto { public int? OrderNumber { get; set; } public OrderStatus? Status { get; set; } public string? CustomerId { get; set; } }
public record OrderReadDto : EntityReadDto { public int OrderNumber { get; set; } public string CustomerDisplay { get; set; } = string.Empty; public OrderStatus Status { get; set; } public FulfillmentStatus FulfillmentStatus { get; set; } public RefundStatus RefundStatus { get; set; } public decimal TotalAmount { get; set; } public string CurrencyCode { get; set; } = "usd"; }
public record CartReadDto : EntityReadDto;
public record StoreReadDto : EntityReadDto { public string Name { get; set; } = string.Empty; public string CurrencyCode { get; set; } = string.Empty; public string? MedusaBaseUrl { get; set; } }
public record StoreWriteDto : EntityWriteDto { public string Name { get; set; } = string.Empty; public string CurrencyCode { get; set; } = string.Empty; public string MedusaBaseUrl { get; set; } = string.Empty; public string AdminApiToken { get; set; } = string.Empty; public string StorefrontPublishableKey { get; set; } = string.Empty; }
public record MerchantRecoveryCartReadDto { public Guid CartId { get; set; } public string CustomerDisplay { get; set; } = string.Empty; public string CustomerId { get; set; } = string.Empty; public bool HasContact { get; set; } public decimal EstimatedValue { get; set; } public decimal TotalAmount { get => EstimatedValue; set => EstimatedValue = value; } public string CurrencyCode { get; set; } = "usd"; public DateTime CreatedAt { get; set; } public DateTime? LastActivityAt { get; set; } public CartRecoveryStatus RecoveryStatus { get; set; } public string RecoveryNote { get; set; } = string.Empty; }

public record RefundRequest(Guid OrderId, string? Note);
public record RecoveryUpdateRequest(Guid CartId, CartRecoveryStatus Status, string? Note);

public interface ICommerceClient
{
    Task<EntityResult<IReadOnlyList<ProductReadDto>>> GetListAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<int>> CountAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<int>> CountAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken);
    Task<IReadOnlyList<MerchantRecoveryCartReadDto>> GetRecoveryQueueAsync(CancellationToken cancellationToken);
    Task<int> CountRecoveryQueueAsync(CancellationToken cancellationToken);
    Task<EntityResult> UpdateRecoveryAsync(Guid cartId, CartRecoveryStatus status, string? note, CancellationToken cancellationToken);
    Task<EntityResult<StoreReadDto>> AddAsync(StoreWriteDto dto, CancellationToken cancellationToken);
    Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken);
    Task<EntityResult> ScheduleCatalogSyncAsync(CancellationToken cancellationToken);
    Task<EntityResult> TriggerCatalogSyncAsync(CancellationToken cancellationToken);
    Task<EntityResult> ScheduleOrderSyncAsync(CancellationToken cancellationToken);
    Task<EntityResult> TriggerOrderSyncAsync(CancellationToken cancellationToken);
    Task<EntityResult> ScheduleCartCleanupAsync(CancellationToken cancellationToken);
}

public static class CommerceApiRoutes
{
    public const string Base = "/api/modules/commerce";
    public const string Products = "/products/query";
    public const string ProductCount = "/products/count";
    public const string Orders = "/orders/query";
    public const string OrderCount = "/orders/count";
    public const string RequestRefund = "/orders/refund";
    public const string Recovery = "/carts/recovery";
    public const string RecoveryCount = "/carts/recovery/count";
    public const string UpdateRecovery = "/carts/recovery/status";
    public const string Stores = "/stores";
    public const string CurrentTenantStore = "/stores/current";
    public const string ScheduleCatalogSync = "/jobs/catalog/schedule";
    public const string TriggerCatalogSync = "/jobs/catalog/trigger";
    public const string ScheduleOrderSync = "/jobs/orders/schedule";
    public const string TriggerOrderSync = "/jobs/orders/trigger";
    public const string ScheduleCartCleanup = "/jobs/carts/cleanup/schedule";
}
