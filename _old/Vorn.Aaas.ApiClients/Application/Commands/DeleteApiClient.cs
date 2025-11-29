using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Infrastructure.Data;

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
    public DeleteApiClientValidator() => RuleFor(c => c.Id).NotEmpty();
}

internal sealed class DeleteApiClientHandler(ApiClientDbContext db) : IRequestHandler<DeleteApiClient>
{
    public async Task Handle(DeleteApiClient request, CancellationToken cancellationToken)
    {
        var client = await db.ApiClients.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if(client is not null)
        {
            db.ApiClients.Remove(client);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
