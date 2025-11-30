using MediatR;
using Vorn.Aaas.RequestIdentities.Application.Queries;
using Vorn.Aaas.RequestIdentities.Presentation.Abstractions;

namespace Vorn.Aaas.RequestIdentities.Infrastructure.Services;

public sealed class RequestIdentitiesService(ISender sender) : IRequestIdentitiesService
{
    public Task<GetRequestIdentitiesResult> GetRequestIdentitiesAsync(
        int page = 0,
        int pageSize = 50,
        string? search = null,
        RequestIdentitiesFilter? filters = null,
        IReadOnlyCollection<RequestIdentitySort>? sorts = null,
        CancellationToken ct = default)
        => sender.Send(new GetRequestIdentities(page, pageSize, search, filters, sorts), ct);
}
