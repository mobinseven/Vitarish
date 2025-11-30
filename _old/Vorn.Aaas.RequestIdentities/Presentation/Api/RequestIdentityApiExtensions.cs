using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vorn.Aaas.RequestIdentities.Application.Queries;
using Vorn.Aaas.RequestIdentities.Presentation.Contracts;

namespace Vorn.Aaas.RequestIdentities.Presentation.Api;

public static partial class RequestIdentityApiExtensions
{
    public static void MapRequestIdentityEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/request-identities")
            .WithTags("RequestIdentities")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        _ = group.MapGet("/", async (
            [AsParameters] GetRequestIdentitiesRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            GetRequestIdentitiesResult result = await mediator.Send(request.ToQuery(), cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetRequestIdentities")
        .Produces<GetRequestIdentitiesResult>(StatusCodes.Status200OK);

        _ = group.MapPost("/", async (
            [FromBody] RequestIdentityDto request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            RequestIdentityDto res = await mediator.Send(request.ToCommand(), cancellationToken);
            return Results.Ok(res);
        })
        .WithName("RecordRequestIdentity")
        .Produces(StatusCodes.Status204NoContent);
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
        public GetRequestIdentities ToQuery()
        {
            RequestIdentitiesFilter? filters = HasFilters
                ? new RequestIdentitiesFilter(RequestId, Authority, ClientId, OwnerId, TenantId, UserId, ResourceId, TraceId, AdditionalInfo)
                : null;

            IReadOnlyCollection<RequestIdentitySort>? sorts = Sort?
                .Select(ParseSort)
                .Where(static sort => sort is not null)
                .Cast<RequestIdentitySort>()
                .ToArray();

            return new GetRequestIdentities(Page, PageSize, Search, filters, sorts);
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
