using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Commerce.Contracts;
using Vitarish.Modules.Base.Endpoints;

namespace Vitarish.Commerce.Api;

public static class CommerceApi
{
    public static IEndpointRouteBuilder MapCommerceApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(CommerceApiRoutes.Base);
        _ = group.MapModulePost(CommerceApiRoutes.Products, (ProductDescriptorDto _, CancellationToken __) => Results.Ok(Array.Empty<ProductReadDto>()), "CommerceSearchProducts", typeof(ProductReadDto[]));
        _ = group.MapModulePost(CommerceApiRoutes.Orders, (OrderDescriptorDto _, CancellationToken __) => Results.Ok(Array.Empty<OrderReadDto>()), "CommerceSearchOrders", typeof(OrderReadDto[]));
        return endpoints;
    }
}
