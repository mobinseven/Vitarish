using Vitarish.Entities.Contracts;

namespace Vitarish.Commerce.Contracts;

public enum ProductStatus { Draft, Published, Archived }
public enum OrderStatus { Pending, Paid, Canceled, Fulfilled }
public enum FulfillmentStatus { Pending, Fulfilled }
public enum RefundStatus { None, Requested, Refunded, Rejected }
public enum CartRecoveryStatus { ContactPending, Contacted, Recovered, Ignored }

public record ProductDescriptorDto : EntityDescriptorDto { public ProductStatus? Status { get; set; } }
public record ProductReadDto : EntityReadDto { public string Title { get; set; } = string.Empty; public string? Description { get; set; } }
public record OrderDescriptorDto : EntityDescriptorDto { public int? OrderNumber { get; set; } public OrderStatus? Status { get; set; } public string? Search { get; set; } public DateTime? CreatedFrom { get; set; } public DateTime? CreatedTo { get; set; } }
public record OrderReadDto : EntityReadDto { public int OrderNumber { get; set; } public string CustomerDisplay { get; set; } = string.Empty; public OrderStatus Status { get; set; } public FulfillmentStatus FulfillmentStatus { get; set; } public RefundStatus RefundStatus { get; set; } public decimal TotalAmount { get; set; } public string CurrencyCode { get; set; } = "usd"; public DateTime CreatedAt { get; set; } }
public record CartReadDto : EntityReadDto;
public record StoreReadDto : EntityReadDto { public string Name { get; set; } = string.Empty; public string CurrencyCode { get; set; } = string.Empty; public string? MedusaBaseUrl { get; set; } }
public record StoreWriteDto : EntityWriteDto { public string Name { get; set; } = string.Empty; public string CurrencyCode { get; set; } = string.Empty; public string MedusaBaseUrl { get; set; } = string.Empty; public string AdminApiToken { get; set; } = string.Empty; public string StorefrontPublishableKey { get; set; } = string.Empty; }
public record MerchantRecoveryCartReadDto { public Guid CartId { get; set; } public string CustomerDisplay { get; set; } = string.Empty; public decimal TotalAmount { get; set; } public string CurrencyCode { get; set; } = "usd"; public CartRecoveryStatus RecoveryStatus { get; set; } }

public interface ICommerceClient : IAccessControlledProductService, IAccessControlledOrderService, ICartService, IStoreService, ICommerceJobManager;
public interface IAccessControlledProductService { Task<EntityResult<IReadOnlyList<ProductReadDto>>> GetListAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken); Task<EntityResult<int>> CountAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken); }
public interface IAccessControlledOrderService { Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken); Task<EntityResult<int>> CountAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken); Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken); }
public interface ICartService { Task<IReadOnlyList<MerchantRecoveryCartReadDto>> GetRecoveryQueueAsync(CancellationToken cancellationToken); Task<int> CountRecoveryQueueAsync(CancellationToken cancellationToken); Task<EntityResult> UpdateRecoveryAsync(Guid cartId, CartRecoveryStatus status, string? note, CancellationToken cancellationToken); }
public interface IStoreService { Task<EntityResult<StoreReadDto>> AddAsync(StoreWriteDto dto, CancellationToken cancellationToken); Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken); }
public interface ICommerceJobManager { void ScheduleCatalogSync(); void TriggerCatalogSync(); void ScheduleOrderSync(); void TriggerOrderSync(); void ScheduleCartCleanup(); }

public static class CommerceApiRoutes { public const string Base = "/api/modules/commerce"; public const string Products = "/products/query"; public const string Orders = "/orders/query"; public const string Recovery = "/carts/recovery"; }
