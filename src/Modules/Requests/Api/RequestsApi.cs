using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Application.Queries;
using Vitarish.Requests.Contracts;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Requests.Api;

public static class RequestsApi
{
    public static IEndpointRouteBuilder MapRequestsApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder requests = endpoints.MapGroup(RequestsRoutes.Base);

        _ = requests.MapGet(RequestsRoutes.Identities, QueryRequestIdentitiesAsync);

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
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.StartsWith("-", StringComparison.Ordinal)
                ? new RequestIdentitySort(value[1..], true)
                : new RequestIdentitySort(value, false))
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
