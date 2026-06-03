using Vitarish.Requests.Application.Queries;

namespace Vitarish.Requests.Application.Abstractions;

public interface IRequestIdentitiesService
{
    Task<GetRequestIdentitiesResult> GetRequestIdentitiesAsync(
        int page = 0,
        int pageSize = 50,
        string? search = null,
        RequestDescriptor? filters = null,
        IReadOnlyCollection<RequestIdentitySort>? sorts = null,
        CancellationToken ct = default);
}
