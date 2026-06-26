using System.Net;
using Vitarish.Commerce.Contracts;
using Vitarish.Entities.Contracts;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Commerce.Client;

public sealed class CommerceClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), ICommerceClient
{
    private static readonly EntityError Unavailable = EntityError.Unknown("Commerce API endpoint is not available for this operation.");
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    public async Task<EntityResult<IReadOnlyList<ProductReadDto>>> GetListAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken) => (await PostJsonAsync<ProductDescriptorDto, IReadOnlyList<ProductReadDto>>(CommerceApiRoutes.Base + CommerceApiRoutes.Products, descriptor, cancellationToken)).ToEntityResult();
    public Task<EntityResult<int>> CountAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken) => Task.FromResult(EntityResult<int>.Failure(Unavailable));
    public async Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken) => (await PostJsonAsync<OrderDescriptorDto, IReadOnlyList<OrderReadDto>>(CommerceApiRoutes.Base + CommerceApiRoutes.Orders, descriptor, cancellationToken)).ToEntityResult();
    public Task<EntityResult<int>> CountAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken) => Task.FromResult(EntityResult<int>.Failure(Unavailable));
    public Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken) => Task.FromResult(EntityResult.Failure(Unavailable));
    public Task<IReadOnlyList<MerchantRecoveryCartReadDto>> GetRecoveryQueueAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MerchantRecoveryCartReadDto>>([]);
    public Task<int> CountRecoveryQueueAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    public Task<EntityResult> UpdateRecoveryAsync(Guid cartId, CartRecoveryStatus status, string? note, CancellationToken cancellationToken) => Task.FromResult(EntityResult.Failure(Unavailable));
    public Task<EntityResult<StoreReadDto>> AddAsync(StoreWriteDto dto, CancellationToken cancellationToken) => Task.FromResult(EntityResult<StoreReadDto>.Failure(Unavailable));
    public Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken) => Task.FromResult<StoreReadDto?>(null);
    public void ScheduleCatalogSync() { } public void TriggerCatalogSync() { } public void ScheduleOrderSync() { } public void TriggerOrderSync() { } public void ScheduleCartCleanup() { }
}
internal static class CommerceClientResultExtensions
{
    public static EntityResult<T> ToEntityResult<T>(this ModuleApiClientResult<T> result) =>
        result.IsSuccess
            ? (result.Value is not null ? EntityResult<T>.Success(result.Value) : EntityResult<T>.Failure(EntityError.Unknown("Response body was empty.")))
            : EntityResult<T>.Failure(EntityError.Unknown(result.Failure?.Message ?? "Commerce API request failed."));
}
