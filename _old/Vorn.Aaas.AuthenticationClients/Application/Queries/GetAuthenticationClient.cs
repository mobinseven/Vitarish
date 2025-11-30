
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Extensions;

namespace Vorn.Aaas.AuthenticationClients.Application.Queries;
/// <summary>
/// Query for a single <see cref="AuthenticationClient"/> by identifier.
/// </summary>
public sealed record GetAuthenticationClient(string Id) : IRequest<AuthenticationClient?>;

/// <summary>
/// Validator for <see cref="GetAuthenticationClient"/>.
/// </summary>
public sealed class GetAuthenticationClientValidator : AbstractValidator<GetAuthenticationClient>
{
    public GetAuthenticationClientValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}

internal sealed class GetAuthenticationClientHandler(AuthenticationClientDbContext db, IDistributedCache cache) : IRequestHandler<GetAuthenticationClient, AuthenticationClient?>
{
    public async Task<AuthenticationClient?> Handle(GetAuthenticationClient request, CancellationToken cancellationToken)
    {
        if (await AuthenticationClientCacheExtensions.GetClientAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        AuthenticationClient? client = await db.AuthenticationClients.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is not null)
        {
            await AuthenticationClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        }

        return client;
    }
}
