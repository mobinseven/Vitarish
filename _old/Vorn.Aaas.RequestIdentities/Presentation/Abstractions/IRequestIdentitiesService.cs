using Vorn.Aaas.RequestIdentities.Application.Queries;

namespace Vorn.Aaas.RequestIdentities.Presentation.Abstractions;

public interface IRequestIdentitiesService
{
    Task<GetRequestIdentitiesResult> GetRequestIdentitiesAsync(
        int page = 0,
        int pageSize = 50,
        string? search = null,
        RequestIdentitiesFilter? filters = null,
        IReadOnlyCollection<RequestIdentitySort>? sorts = null,
        CancellationToken ct = default);
}
