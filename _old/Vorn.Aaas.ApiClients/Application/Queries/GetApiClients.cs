
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Infrastructure.Extensions;

namespace Vorn.Aaas.ApiClients.Application.Queries;
/// <summary>
/// Query for all API clients.
/// </summary>
public sealed record GetApiClients : IRequest<IEnumerable<ApiClient>>;

/// <summary>
/// Validator for <see cref="GetApiClients"/>.
/// </summary>
public sealed class GetApiClientsValidator : AbstractValidator<GetApiClients> { }

internal sealed class GetApiClientsHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<GetApiClients, IEnumerable<ApiClient>>
{
    public async Task<IEnumerable<ApiClient>> Handle(GetApiClients request, CancellationToken cancellationToken)
    {
        if (await ApiClientCacheExtensions.GetClientsAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        ApiClient[] clients = await db.ApiClients.AsNoTracking().ToArrayAsync(cancellationToken);
        await ApiClientCacheExtensions.SetClientsAsync(cache, clients, cancellationToken);
        return clients;
    }
}
