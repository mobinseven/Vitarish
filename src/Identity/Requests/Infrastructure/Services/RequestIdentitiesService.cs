using MediatR;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Application.Queries;

namespace Vitarish.Requests.Infrastructure.Services;

public sealed class RequestIdentitiesService(ISender sender) : IRequestIdentitiesService
{
    public Task<GetRequestIdentitiesResult> GetRequestIdentitiesAsync(
        int page = 0,
        int pageSize = 50,
        string? search = null,
        RequestDescriptor? filters = null,
        IReadOnlyCollection<RequestIdentitySort>? sorts = null,
        CancellationToken ct = default)
        => sender.Send(new GetRequestsCommand(page, pageSize, search, filters, sorts), ct);
}
