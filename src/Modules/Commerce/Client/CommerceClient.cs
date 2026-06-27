using System.Net;
using Vitarish.Commerce.Contracts;
using Vitarish.Entities.Contracts;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Commerce.Client;

public sealed class CommerceClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), ICommerceClient
{
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);

    public async Task<EntityResult<IReadOnlyList<ProductReadDto>>> GetListAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken) => (await PostJsonAsync<ProductDescriptorDto, IReadOnlyList<ProductReadDto>>(CommerceApiRoutes.Base + CommerceApiRoutes.Products, descriptor, cancellationToken)).ToEntityResult();
    public async Task<EntityResult<int>> CountAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken) => (await PostJsonAsync<ProductDescriptorDto, int>(CommerceApiRoutes.Base + CommerceApiRoutes.ProductCount, descriptor, cancellationToken)).ToEntityResult();
    public async Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken) => (await PostJsonAsync<OrderDescriptorDto, IReadOnlyList<OrderReadDto>>(CommerceApiRoutes.Base + CommerceApiRoutes.Orders, descriptor, cancellationToken)).ToEntityResult();
    public async Task<EntityResult<int>> CountAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken) => (await PostJsonAsync<OrderDescriptorDto, int>(CommerceApiRoutes.Base + CommerceApiRoutes.OrderCount, descriptor, cancellationToken)).ToEntityResult();
    public async Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken) => (await PostJsonAsync<RefundRequest, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.RequestRefund, new(orderId, note), cancellationToken)).ToCommandResult();
    public async Task<IReadOnlyList<MerchantRecoveryCartReadDto>> GetRecoveryQueueAsync(CancellationToken cancellationToken) => (await GetJsonAsync<IReadOnlyList<MerchantRecoveryCartReadDto>>(CommerceApiRoutes.Base + CommerceApiRoutes.Recovery, cancellationToken)).Value ?? [];
    public async Task<int> CountRecoveryQueueAsync(CancellationToken cancellationToken) => (await GetJsonAsync<int>(CommerceApiRoutes.Base + CommerceApiRoutes.RecoveryCount, cancellationToken)).Value;
    public async Task<EntityResult> UpdateRecoveryAsync(Guid cartId, CartRecoveryStatus status, string? note, CancellationToken cancellationToken) => (await PostJsonAsync<RecoveryUpdateRequest, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.UpdateRecovery, new(cartId, status, note), cancellationToken)).ToCommandResult();
    public async Task<EntityResult<StoreReadDto>> AddAsync(StoreWriteDto dto, CancellationToken cancellationToken) => (await PostJsonAsync<StoreWriteDto, StoreReadDto>(CommerceApiRoutes.Base + CommerceApiRoutes.Stores, dto, cancellationToken)).ToEntityResult();
    public async Task<StoreReadDto?> GetStoreByTenantAsync(CancellationToken cancellationToken) => (await GetJsonAsync<StoreReadDto>(CommerceApiRoutes.Base + CommerceApiRoutes.CurrentTenantStore, cancellationToken)).Value;
    public async Task<EntityResult> ScheduleCatalogSyncAsync(CancellationToken cancellationToken) => (await PostJsonAsync<object, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.ScheduleCatalogSync, new { }, cancellationToken)).ToCommandResult();
    public async Task<EntityResult> TriggerCatalogSyncAsync(CancellationToken cancellationToken) => (await PostJsonAsync<object, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.TriggerCatalogSync, new { }, cancellationToken)).ToCommandResult();
    public async Task<EntityResult> ScheduleOrderSyncAsync(CancellationToken cancellationToken) => (await PostJsonAsync<object, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.ScheduleOrderSync, new { }, cancellationToken)).ToCommandResult();
    public async Task<EntityResult> TriggerOrderSyncAsync(CancellationToken cancellationToken) => (await PostJsonAsync<object, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.TriggerOrderSync, new { }, cancellationToken)).ToCommandResult();
    public async Task<EntityResult> ScheduleCartCleanupAsync(CancellationToken cancellationToken) => (await PostJsonAsync<object, EntityResult>(CommerceApiRoutes.Base + CommerceApiRoutes.ScheduleCartCleanup, new { }, cancellationToken)).ToCommandResult();
}

internal static class CommerceClientResultExtensions
{
    public static EntityResult<T> ToEntityResult<T>(this ModuleApiClientResult<T> result) =>
        result.IsSuccess
            ? (result.Value is not null ? EntityResult<T>.Success(result.Value) : EntityResult<T>.Failure(EntityError.Unknown("Response body was empty.")))
            : EntityResult<T>.Failure(EntityError.Unknown(result.Failure?.Message ?? "Commerce API request failed."));

    public static EntityResult ToCommandResult(this ModuleApiClientResult<EntityResult> result) =>
        result.IsSuccess ? result.Value : EntityResult.Failure(EntityError.Unknown(result.Failure?.Message ?? "Commerce API request failed."));
}
