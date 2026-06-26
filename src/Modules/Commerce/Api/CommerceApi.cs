using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Commerce.Contracts;
using Vitarish.Modules.Base.Endpoints;

namespace Vitarish.Commerce.Api;
public static class CommerceApi { public static IEndpointRouteBuilder MapCommerceApi(this IEndpointRouteBuilder endpoints) { RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(CommerceApiRoutes.Base); _ = group.MapPost(CommerceApiRoutes.Products, (ProductDescriptorDto _, CancellationToken __) => Results.Ok(Array.Empty<ProductReadDto>())); _ = group.MapPost(CommerceApiRoutes.Orders, (OrderDescriptorDto _, CancellationToken __) => Results.Ok(Array.Empty<OrderReadDto>())); return endpoints; } }
