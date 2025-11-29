using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;

namespace Vorn.Aaas.ApiClients.Application.Queries;

/// <summary>
/// Query for all API clients.
/// </summary>
public sealed record GetApiClients : IRequest<IEnumerable<ApiClient>>;

/// <summary>
/// Validator for <see cref="GetApiClients"/> (no rules needed).
/// </summary>
public sealed class GetApiClientsValidator : AbstractValidator<GetApiClients> { }

internal sealed class GetApiClientsHandler(ApiClientDbContext db) : IRequestHandler<GetApiClients, IEnumerable<ApiClient>>
{
    public async Task<IEnumerable<ApiClient>> Handle(GetApiClients request, CancellationToken cancellationToken) => await db.ApiClients.AsNoTracking()
            .ToListAsync(cancellationToken);
}
