using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Application.Queries;
using Vitarish.Requests.Contracts;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Requests.Api;

public static class RequestsApi
{
    public static IEndpointRouteBuilder MapRequestsApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder requests = endpoints.MapProtectedModuleGroup(RequestsRoutes.Base).WithTags("Requests");

        _ = requests.MapModuleGet(RequestsRoutes.Identities, QueryRequestIdentitiesAsync, "RequestsQueryRequestIdentities");

        return endpoints;
    }

    private static async Task<IResult> QueryRequestIdentitiesAsync(
        [AsParameters] RequestIdentityQueryParameters parameters,
        HttpRequest httpRequest,
        IRequestIdentitiesService requestIdentities,
        CancellationToken cancellationToken)
    {
        RequestDescriptor filters = ToFilters(parameters);
        IReadOnlyCollection<RequestIdentitySort> sorts = ToSorts(httpRequest.Query["sort"]);

        GetRequestIdentitiesResult result = await requestIdentities.GetRequestIdentitiesAsync(
            parameters.Page ?? 0,
            parameters.PageSize ?? 50,
            parameters.Search,
            filters,
            sorts,
            cancellationToken);

        return Results.Ok(new RequestIdentityPageResponse(
            result.Items.Select(ToResponse).ToArray(),
            result.TotalItems,
            parameters.Page ?? 0,
            parameters.PageSize ?? 50));
    }

    private static RequestDescriptor ToFilters(RequestIdentityQueryParameters parameters) => new(
        parameters.RequestId,
        parameters.Authority,
        parameters.ClientId,
        parameters.OwnerId,
        parameters.TenantId,
        parameters.UserId,
        parameters.ResourceId,
        parameters.TraceId,
        parameters.AdditionalInfo);

    private static IReadOnlyCollection<RequestIdentitySort> ToSorts(IReadOnlyList<string> sortValues)
        => sortValues
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.StartsWith("-", StringComparison.Ordinal)
                ? (Field: value[1..].Trim(), Descending: true)
                : (Field: value, Descending: false))
            .Where(x => !string.IsNullOrWhiteSpace(x.Field))
            .Select(x => new RequestIdentitySort(x.Field, x.Descending))
            .ToArray();

    private static RequestIdentityResponse ToResponse(Request request) => new(
        request.Id,
        request.RequestId,
        request.Authority,
        request.ClientId,
        request.OwnerId,
        request.TenantId,
        request.UserId,
        request.ResourceId,
        request.TraceId,
        request.AdditionalInfo,
        request.CreatedAt);
}
