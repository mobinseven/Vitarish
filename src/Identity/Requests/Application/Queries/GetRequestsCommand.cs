using MediatR;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Requests.Application.Queries;

public sealed record RequestDescriptor(
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

public sealed record GetRequestsCommand(
    int Page = 0,
    int PageSize = 50,
    string? Search = null,
    RequestDescriptor? Filters = null,
    IReadOnlyCollection<RequestIdentitySort>? Sorts = null)
    : IRequest<GetRequestIdentitiesResult>;

public sealed record GetRequestIdentitiesResult(IReadOnlyList<Request> Items, int TotalItems);
