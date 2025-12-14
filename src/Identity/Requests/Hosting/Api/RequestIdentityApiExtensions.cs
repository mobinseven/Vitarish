using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Requests.Application.Extensions;
using Vitarish.Requests.Application.Queries;
using Vitarish.Requests.Domain.Models;

namespace Vitarish.Requests.Hosting.Api;

public static partial class RequestIdentityApiExtensions
{
    public static IEndpointConventionBuilder MapRequestIdentityEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/request-identities")
            .WithTags("Requests");

        _ = group.MapGet("/", GetRequestIdentitiesAsync)
            .WithName("GetRequestsCommand")
            .Produces<GetRequestIdentitiesResult>(StatusCodes.Status200OK)
            .WithSummary("List request identities")
            .WithDescription("Retrieves request identities using optional paging, search, and filter parameters.");

        _ = group.MapPost("/", RecordRequestIdentityAsync)
            .WithName("RecordRequestIdentity")
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Record a request identity")
            .WithDescription("Stores a request identity entry using the supplied payload.");

        return group;
    }

    /// <summary>
    /// Retrieves request identities using the supplied filter and paging parameters.
    /// </summary>
    /// <param name="request">The query parameters used to filter and page the results.</param>
    /// <param name="mediator">The mediator used to dispatch the query.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A 200 response containing the filtered result set.</returns>
    static async Task<IResult> GetRequestIdentitiesAsync(
        [AsParameters] GetRequestIdentitiesRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        GetRequestIdentitiesResult result = await mediator.Send(request.ToQuery(), cancellationToken);
        return Results.Ok(result);
    }

    /// <summary>
    /// Records a request identity using the provided payload.
    /// </summary>
    /// <param name="request">The request identity details to persist.</param>
    /// <param name="mediator">The mediator used to dispatch the command.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A 200 response containing the recorded request identity.</returns>
    static async Task<IResult> RecordRequestIdentityAsync(
        [FromBody] RequestIdentityDto request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        RequestIdentityDto res = await mediator.Send(request.ToCommand(), cancellationToken);
        return Results.Ok(res);
    }
    private sealed record GetRequestIdentitiesRequest(
        [FromQuery(Name = "page")] int Page = 0,
        [FromQuery(Name = "pageSize")] int PageSize = 50,
        [FromQuery(Name = "search")] string? Search = null,
        [FromQuery(Name = "requestId")] string? RequestId = null,
        [FromQuery(Name = "authority")] string? Authority = null,
        [FromQuery(Name = "clientId")] string? ClientId = null,
        [FromQuery(Name = "ownerId")] string? OwnerId = null,
        [FromQuery(Name = "tenantId")] string? TenantId = null,
        [FromQuery(Name = "userId")] string? UserId = null,
        [FromQuery(Name = "resourceId")] string? ResourceId = null,
        [FromQuery(Name = "traceId")] string? TraceId = null,
        [FromQuery(Name = "additionalInfo")] string? AdditionalInfo = null,
        [FromQuery(Name = "sort")] string[]? Sort = null)
    {
        public GetRequestsCommand ToQuery()
        {
            RequestDescriptor? filters = HasFilters
                ? new RequestDescriptor(RequestId, Authority, ClientId, OwnerId, TenantId, UserId, ResourceId, TraceId, AdditionalInfo)
                : null;

            IReadOnlyCollection<RequestIdentitySort>? sorts = Sort?
                .Select(ParseSort)
                .Where(static sort => sort is not null)
                .Cast<RequestIdentitySort>()
                .ToArray();

            return new GetRequestsCommand(Page, PageSize, Search, filters, sorts);
        }

        private bool HasFilters
            => !string.IsNullOrWhiteSpace(RequestId)
               || !string.IsNullOrWhiteSpace(Authority)
               || !string.IsNullOrWhiteSpace(ClientId)
               || !string.IsNullOrWhiteSpace(OwnerId)
               || !string.IsNullOrWhiteSpace(TenantId)
               || !string.IsNullOrWhiteSpace(UserId)
               || !string.IsNullOrWhiteSpace(ResourceId)
               || !string.IsNullOrWhiteSpace(TraceId)
               || !string.IsNullOrWhiteSpace(AdditionalInfo);

        private static RequestIdentitySort? ParseSort(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string[] parts = raw.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return null;
            }

            string field = parts[0];
            bool descending = parts.Length == 1 || parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            return new RequestIdentitySort(field, descending);
        }
    }
}
