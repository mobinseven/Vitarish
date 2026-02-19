using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Application.Queries;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Main.Mock.Services;

public sealed class MockRequestIdentitiesService : IRequestIdentitiesService
{
    public Task<GetRequestIdentitiesResult> GetRequestIdentitiesAsync(
        int page = 0,
        int pageSize = 50,
        string? search = null,
        RequestDescriptor? filters = null,
        IReadOnlyCollection<RequestIdentitySort>? sorts = null,
        CancellationToken ct = default) => Task.FromResult(new GetRequestIdentitiesResult(Array.Empty<Request>(), 0));
}
