using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Commerce.Application.Abstractions;
using Vitarish.Commerce.Contracts;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Commerce.Api;

public static class CommerceApi
{
    public static IEndpointRouteBuilder MapCommerceApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(CommerceApiRoutes.Base).WithTags("Commerce");
        _ = group.MapModulePost(CommerceApiRoutes.Products, SearchProductsAsync, "CommerceSearchProducts", typeof(ProductReadDto[]));
        _ = group.MapModulePost(CommerceApiRoutes.ProductCount, CountProductsAsync, "CommerceCountProducts", typeof(int));
        _ = group.MapModulePost(CommerceApiRoutes.Orders, SearchOrdersAsync, "CommerceSearchOrders", typeof(OrderReadDto[]));
        _ = group.MapModulePost(CommerceApiRoutes.OrderCount, CountOrdersAsync, "CommerceCountOrders", typeof(int));
        _ = group.MapModulePost(CommerceApiRoutes.RequestRefund, RequestRefundAsync, "CommerceRequestRefund", typeof(EntityResult));
        _ = group.MapModuleGet(CommerceApiRoutes.Recovery, GetRecoveryQueueAsync, "CommerceRecoveryQueue", typeof(MerchantRecoveryCartReadDto[]));
        _ = group.MapModuleGet(CommerceApiRoutes.RecoveryCount, CountRecoveryQueueAsync, "CommerceRecoveryQueueCount", typeof(int));
        _ = group.MapModulePost(CommerceApiRoutes.UpdateRecovery, UpdateRecoveryAsync, "CommerceUpdateRecovery", typeof(EntityResult));
        _ = group.MapModulePost(CommerceApiRoutes.Stores, AddStoreAsync, "CommerceAddStore", typeof(StoreReadDto));
        _ = group.MapModuleGet(CommerceApiRoutes.CurrentTenantStore, GetCurrentTenantStoreAsync, "CommerceCurrentTenantStore", typeof(StoreReadDto));
        _ = group.MapModulePost(CommerceApiRoutes.ScheduleCatalogSync, (ICommerceJobManager jobs) => RunJob(jobs.ScheduleCatalogSync), "CommerceScheduleCatalogSync", typeof(EntityResult));
        _ = group.MapModulePost(CommerceApiRoutes.TriggerCatalogSync, (ICommerceJobManager jobs) => RunJob(jobs.TriggerCatalogSync), "CommerceTriggerCatalogSync", typeof(EntityResult));
        _ = group.MapModulePost(CommerceApiRoutes.ScheduleOrderSync, (ICommerceJobManager jobs) => RunJob(jobs.ScheduleOrderSync), "CommerceScheduleOrderSync", typeof(EntityResult));
        _ = group.MapModulePost(CommerceApiRoutes.TriggerOrderSync, (ICommerceJobManager jobs) => RunJob(jobs.TriggerOrderSync), "CommerceTriggerOrderSync", typeof(EntityResult));
        _ = group.MapModulePost(CommerceApiRoutes.ScheduleCartCleanup, (ICommerceJobManager jobs) => RunJob(jobs.ScheduleCartCleanup), "CommerceScheduleCartCleanup", typeof(EntityResult));
        return endpoints;
    }

    private static async Task<IResult> SearchProductsAsync(ProductDescriptorDto? request, IAccessControlledProductService service, CancellationToken cancellationToken) => ToOkList(await service.GetListAsync(CommerceContractMapper.ToDomain(request ?? new()), cancellationToken), CommerceContractMapper.ToContract);
    private static async Task<IResult> CountProductsAsync(ProductDescriptorDto? request, IAccessControlledProductService service, CancellationToken cancellationToken) => ToOk(await service.CountAsync(CommerceContractMapper.ToDomain(request ?? new()), cancellationToken));
    private static async Task<IResult> SearchOrdersAsync(OrderDescriptorDto? request, IAccessControlledOrderService service, CancellationToken cancellationToken) => ToOkList(await service.GetListAsync(CommerceContractMapper.ToDomain(request ?? new()), cancellationToken), CommerceContractMapper.ToContract);
    private static async Task<IResult> CountOrdersAsync(OrderDescriptorDto? request, IAccessControlledOrderService service, CancellationToken cancellationToken) => ToOk(await service.CountAsync(CommerceContractMapper.ToDomain(request ?? new()), cancellationToken));
    private static async Task<IResult> RequestRefundAsync(RefundRequest? request, IAccessControlledOrderService service, CancellationToken cancellationToken) => request is null || request.OrderId == Guid.Empty ? Results.BadRequest("OrderId is required.") : ToOk(await service.RequestRefundAsync(request.OrderId, request.Note, cancellationToken));
    private static async Task<IResult> GetRecoveryQueueAsync(ICartService service, CancellationToken cancellationToken) => Results.Ok((await service.GetRecoveryQueueAsync(cancellationToken)).Select(CommerceContractMapper.ToContract).ToArray());
    private static async Task<IResult> CountRecoveryQueueAsync(ICartService service, CancellationToken cancellationToken) => Results.Ok(await service.CountRecoveryQueueAsync(cancellationToken));
    private static async Task<IResult> UpdateRecoveryAsync(RecoveryUpdateRequest? request, ICartService service, CancellationToken cancellationToken) => request is null || request.CartId == Guid.Empty ? Results.BadRequest("CartId is required.") : ToOk(await service.UpdateRecoveryAsync(request.CartId, CommerceContractMapper.MapCartRecoveryStatus(request.Status), request.Note, cancellationToken));
    private static async Task<IResult> AddStoreAsync(StoreWriteDto? request, IStoreService service, CancellationToken cancellationToken) => request is null ? Results.BadRequest("Store data is required.") : ToOk(await service.AddAsync(CommerceContractMapper.ToDomain(request), cancellationToken), CommerceContractMapper.ToContract);
    private static async Task<IResult> GetCurrentTenantStoreAsync(IStoreService service, CancellationToken cancellationToken) { var store = await service.GetStoreByTenantAsync(cancellationToken); return store is null ? Results.NotFound() : Results.Ok(CommerceContractMapper.ToContract(store)); }
    private static IResult RunJob(Action action) { action(); return Results.Ok(EntityResult.Success()); }

    private static IResult ToOk(EntityResult result) => result.IsSuccess ? Results.Ok(result) : Results.Problem(string.Join("; ", result.Errors.Select(e => e.Message)));
    private static IResult ToOk<T>(EntityResult<T> result) => result.IsSuccess && result.Value is not null ? Results.Ok(result.Value) : Results.Problem(string.Join("; ", result.Errors.Select(e => e.Message)));
    private static IResult ToOk<TIn, TOut>(EntityResult<TIn> result, Func<TIn, TOut> map) => result.IsSuccess && result.Value is not null ? Results.Ok(map(result.Value)) : Results.Problem(string.Join("; ", result.Errors.Select(e => e.Message)));
    private static IResult ToOkList<TIn, TOut>(EntityResult<IReadOnlyList<TIn>> result, Func<TIn, TOut> map) => result.IsSuccess && result.Value is not null ? Results.Ok(result.Value.Select(map).ToArray()) : Results.Problem(string.Join("; ", result.Errors.Select(e => e.Message)));
}
