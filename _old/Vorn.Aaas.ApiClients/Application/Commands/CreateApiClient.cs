using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Domain.Events;
using Vorn.Aaas.ApiClients.Infrastructure.Data;

namespace Vorn.Aaas.ApiClients.Application.Commands;

/// <summary>
/// Command to create a new <see cref="ApiClient"/>.
/// </summary>
public sealed record CreateApiClient(string Id, string Secret, string Scopes, string HostUri) : IRequest<ApiClient?>;

/// <summary>
/// Validator for <see cref="CreateApiClient"/>.
/// </summary>
public class CreateApiClientValidator : AbstractValidator<CreateApiClient>
{
    public CreateApiClientValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Secret).NotEmpty();
        RuleFor(c => c.Scopes).NotEmpty();
        RuleFor(c => c.HostUri).NotEmpty();
    }
}

internal sealed class CreateApiClientHandler(ApiClientDbContext db, IPublisher publisher) : IRequestHandler<CreateApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(CreateApiClient request, CancellationToken cancellationToken)
    {
        if (await db.ApiClients.AnyAsync(a => a.Id == request.Id, cancellationToken))
        {
            return null;
        }

        ApiClient client = new()
        {
            Id = request.Id,
            Secret = request.Secret,
            Scopes = request.Scopes,
            HostUri = request.HostUri
        };

        db.ApiClients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new ApiClientCreatedEvent(client.Id), cancellationToken);
        return client;
    }
}
