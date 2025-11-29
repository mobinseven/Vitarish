using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;

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
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Secret).NotEmpty();
        RuleFor(c => c.Scopes).NotEmpty();
        RuleFor(c => c.HostUri).NotEmpty();
    }
}

internal sealed class UpdateApiClientHandler(ApiClientDbContext db) : IRequestHandler<UpdateApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(UpdateApiClient request, CancellationToken cancellationToken)
    {
        var client = await db.ApiClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (client is null)
        {
            return null;
        }

        client.Secret = request.Secret;
        client.Scopes = request.Scopes;
        client.HostUri = request.HostUri;

        await db.SaveChangesAsync(cancellationToken);
        return client;
    }
}
