
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Infrastructure.Extensions;

namespace Vorn.Aaas.ApiClients.Application.Commands;
/// <summary>
/// Command to remove an API client by identifier.
/// </summary>
public sealed record DeleteApiClient(string Id) : IRequest;

/// <summary>
/// Validator for <see cref="DeleteApiClient"/>.
/// </summary>
public class DeleteApiClientValidator : AbstractValidator<DeleteApiClient>
{
    public DeleteApiClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}

internal sealed class DeleteApiClientHandler(ApiClientDbContext db, IDistributedCache cache) : IRequestHandler<DeleteApiClient>
{
    public async Task Handle(DeleteApiClient request, CancellationToken cancellationToken)
    {
        ApiClient? client = await db.ApiClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return;
        }

        _ = db.ApiClients.Remove(client);
        _ = await db.SaveChangesAsync(cancellationToken);
        await ApiClientCacheExtensions.RemoveClientAsync(cache, client.Id, cancellationToken);
        await ApiClientCacheExtensions.RemoveClientsAsync(cache, cancellationToken);
    }
}
