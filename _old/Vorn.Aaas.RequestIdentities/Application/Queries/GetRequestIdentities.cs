using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.RequestIdentities.Domain.Entities;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;

namespace Vorn.Aaas.RequestIdentities.Application.Queries;

public sealed record RequestIdentitiesFilter(
    string? RequestId = null,
    string? Authority = null,
    string? ClientId = null,
    string? OwnerId = null,
    string? TenantId = null,
    string? UserId = null,
    string? ResourceId = null,
    string? TraceId = null,
    string? AdditionalInfo = null);

public sealed record RequestIdentitySort(string Field, bool Descending);

public sealed record GetRequestIdentities(
    int Page = 0,
    int PageSize = 50,
    string? Search = null,
    RequestIdentitiesFilter? Filters = null,
    IReadOnlyCollection<RequestIdentitySort>? Sorts = null)
    : IRequest<GetRequestIdentitiesResult>;

public sealed record GetRequestIdentitiesResult(IReadOnlyList<RequestIdentity> Items, int TotalItems);

internal sealed class GetRequestIdentitiesHandler(RequestIdentityDbContext dbContext)
    : IRequestHandler<GetRequestIdentities, GetRequestIdentitiesResult>
{
    public async Task<GetRequestIdentitiesResult> Handle(GetRequestIdentities request, CancellationToken cancellationToken)
    {
        int page = Math.Max(0, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 500);

        IQueryable<RequestIdentity> query = dbContext.RequestIdentities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string pattern = $"%{request.Search.Trim()}%";
            query = query.Where(identity =>
                EF.Functions.Like(identity.RequestId, pattern) ||
                EF.Functions.Like(identity.Authority, pattern) ||
                EF.Functions.Like(identity.ClientId, pattern) ||
                EF.Functions.Like(identity.OwnerId, pattern) ||
                (identity.TenantId != null && EF.Functions.Like(identity.TenantId, pattern)) ||
                (identity.UserId != null && EF.Functions.Like(identity.UserId, pattern)) ||
                (identity.ResourceId != null && EF.Functions.Like(identity.ResourceId, pattern)) ||
                (identity.TraceId != null && EF.Functions.Like(identity.TraceId, pattern)) ||
                (identity.AdditionalInfo != null && EF.Functions.Like(identity.AdditionalInfo, pattern)));
        }

        if (request.Filters is { } filters)
        {
            query = ApplyFilters(query, filters);
        }

        IOrderedQueryable<RequestIdentity> sortedQuery = ApplySorts(query, request.Sorts);

        int totalItems = await sortedQuery.CountAsync(cancellationToken);
        List<RequestIdentity> items = await sortedQuery
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new GetRequestIdentitiesResult(items, totalItems);
    }

    private static IQueryable<RequestIdentity> ApplyFilters(IQueryable<RequestIdentity> source, RequestIdentitiesFilter filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.RequestId))
        {
            string pattern = $"%{filters.RequestId.Trim()}%";
            source = source.Where(identity => EF.Functions.Like(identity.RequestId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.Authority))
        {
            string pattern = $"%{filters.Authority.Trim()}%";
            source = source.Where(identity => EF.Functions.Like(identity.Authority, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.ClientId))
        {
            string pattern = $"%{filters.ClientId.Trim()}%";
            source = source.Where(identity => EF.Functions.Like(identity.ClientId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.OwnerId))
        {
            string pattern = $"%{filters.OwnerId.Trim()}%";
            source = source.Where(identity => EF.Functions.Like(identity.OwnerId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.TenantId))
        {
            string pattern = $"%{filters.TenantId.Trim()}%";
            source = source.Where(identity => identity.TenantId != null && EF.Functions.Like(identity.TenantId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.UserId))
        {
            string pattern = $"%{filters.UserId.Trim()}%";
            source = source.Where(identity => identity.UserId != null && EF.Functions.Like(identity.UserId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.ResourceId))
        {
            string pattern = $"%{filters.ResourceId.Trim()}%";
            source = source.Where(identity => identity.ResourceId != null && EF.Functions.Like(identity.ResourceId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.TraceId))
        {
            string pattern = $"%{filters.TraceId.Trim()}%";
            source = source.Where(identity => identity.TraceId != null && EF.Functions.Like(identity.TraceId, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filters.AdditionalInfo))
        {
            string pattern = $"%{filters.AdditionalInfo.Trim()}%";
            source = source.Where(identity => identity.AdditionalInfo != null && EF.Functions.Like(identity.AdditionalInfo, pattern));
        }

        return source;
    }

    private static IOrderedQueryable<RequestIdentity> ApplySorts(IQueryable<RequestIdentity> source, IReadOnlyCollection<RequestIdentitySort>? sorts)
    {
        IOrderedQueryable<RequestIdentity>? ordered = null;

        if (sorts is not null)
        {
            foreach (RequestIdentitySort sort in sorts)
            {
                ordered = ordered is null
                    ? TryOrderBy(source, sort)
                    : TryThenBy(ordered, sort) ?? ordered;
            }
        }

        return ordered ?? source.OrderByDescending(identity => identity.CreatedAt);
    }

    private static IOrderedQueryable<RequestIdentity>? TryOrderBy(IQueryable<RequestIdentity> source, RequestIdentitySort sort)
        => sort.Field switch
        {
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.RequestId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.RequestId) : source.OrderBy(identity => identity.RequestId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.Authority), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.Authority) : source.OrderBy(identity => identity.Authority),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.ClientId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.ClientId) : source.OrderBy(identity => identity.ClientId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.OwnerId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.OwnerId) : source.OrderBy(identity => identity.OwnerId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.TenantId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.TenantId) : source.OrderBy(identity => identity.TenantId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.UserId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.UserId) : source.OrderBy(identity => identity.UserId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.ResourceId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.ResourceId) : source.OrderBy(identity => identity.ResourceId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.TraceId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.TraceId) : source.OrderBy(identity => identity.TraceId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.AdditionalInfo), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.AdditionalInfo) : source.OrderBy(identity => identity.AdditionalInfo),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.CreatedAt), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.OrderByDescending(identity => identity.CreatedAt) : source.OrderBy(identity => identity.CreatedAt),
            _ => null
        };

    private static IOrderedQueryable<RequestIdentity>? TryThenBy(IOrderedQueryable<RequestIdentity> source, RequestIdentitySort sort)
        => sort.Field switch
        {
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.RequestId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.RequestId) : source.ThenBy(identity => identity.RequestId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.Authority), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.Authority) : source.ThenBy(identity => identity.Authority),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.ClientId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.ClientId) : source.ThenBy(identity => identity.ClientId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.OwnerId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.OwnerId) : source.ThenBy(identity => identity.OwnerId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.TenantId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.TenantId) : source.ThenBy(identity => identity.TenantId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.UserId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.UserId) : source.ThenBy(identity => identity.UserId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.ResourceId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.ResourceId) : source.ThenBy(identity => identity.ResourceId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.TraceId), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.TraceId) : source.ThenBy(identity => identity.TraceId),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.AdditionalInfo), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.AdditionalInfo) : source.ThenBy(identity => identity.AdditionalInfo),
            { Length: > 0 } field when field.Equals(nameof(RequestIdentity.CreatedAt), StringComparison.OrdinalIgnoreCase)
                => sort.Descending ? source.ThenByDescending(identity => identity.CreatedAt) : source.ThenBy(identity => identity.CreatedAt),
            _ => null
        };
}
