
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Infrastructure.Extensions;

namespace Vorn.Aaas.ApiClients.Application.Queries;
/// <summary>
/// Query for a single <see cref="ApiClient"/> by identifier.
/// </summary>
public sealed record GetApiClient(string Id) : IRequest<ApiClient?>;

/// <summary>
/// Validator for <see cref="GetApiClient"/>.
/// </summary>
public sealed class GetApiClientValidator : AbstractValidator<GetApiClient>
{
    public GetApiClientValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}

internal sealed class GetApiClientHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<GetApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(GetApiClient request, CancellationToken cancellationToken)
    {
        if (await ApiClientCacheExtensions.GetClientAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        ApiClient? entity = await db.ApiClients.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (entity is not null)
        {
            await ApiClientCacheExtensions.SetClientAsync(cache, entity, cancellationToken);
        }

        return entity;
    }
}
