
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Extensions;

namespace Vorn.Aaas.AuthenticationClients.Application.Queries;
/// <summary>
/// Query for all authentication clients.
/// </summary>
public sealed record GetAuthenticationClients : IRequest<IEnumerable<AuthenticationClient>>;

/// <summary>
/// Validator for <see cref="GetAuthenticationClients"/>.
/// </summary>
public sealed class GetAuthenticationClientsValidator : AbstractValidator<GetAuthenticationClients> { }

internal sealed class GetAuthenticationClientsHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<GetAuthenticationClients, IEnumerable<AuthenticationClient>>
{
    public async Task<IEnumerable<AuthenticationClient>> Handle(GetAuthenticationClients request, CancellationToken cancellationToken)
    {
        if (await AuthenticationClientCacheExtensions.GetClientsAsync(cache, cancellationToken) is { } cached)
        {
            return cached;
        }

        AuthenticationClient[] clients = await db.AuthenticationClients.AsNoTracking().ToArrayAsync(cancellationToken);
        await AuthenticationClientCacheExtensions.SetClientsAsync(cache, clients, cancellationToken);
        return clients;
    }
}
