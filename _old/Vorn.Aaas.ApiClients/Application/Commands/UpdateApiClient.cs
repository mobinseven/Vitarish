
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Infrastructure.Extensions;

namespace Vorn.Aaas.ApiClients.Application.Commands;
/// <summary>
/// Command to edit an existing <see cref="ApiClient"/>.
/// </summary>
public sealed record UpdateApiClient(string Id, string Secret, string Scopes, string HostUri) : IRequest<ApiClient?>;

/// <summary>
/// Validator for <see cref="UpdateApiClient"/>.
/// </summary>
public class UpdateApiClientValidator : AbstractValidator<UpdateApiClient>
{
    public UpdateApiClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Secret).NotEmpty();
        _ = RuleFor(c => c.Scopes).NotEmpty();
        _ = RuleFor(c => c.HostUri).NotEmpty();
    }
}

internal sealed class UpdateApiClientHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<UpdateApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(UpdateApiClient request, CancellationToken cancellationToken)
    {
        ApiClient? client = await db.ApiClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return null;
        }

        client.Secret = request.Secret;
        client.Scopes = request.Scopes;
        client.HostUri = request.HostUri;

        _ = await db.SaveChangesAsync(cancellationToken);
        await ApiClientCacheExtensions.SetClientAsync(cache, client, cancellationToken);
        await ApiClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
        return client;
    }
}
