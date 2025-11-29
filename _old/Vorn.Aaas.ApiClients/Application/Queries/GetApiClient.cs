using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;

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
    public GetApiClientValidator() => RuleFor(q => q.Id).NotEmpty();
}

internal sealed class GetApiClientHandler(ApiClientDbContext db) : IRequestHandler<GetApiClient, ApiClient?>
{
    public async Task<ApiClient?> Handle(GetApiClient request, CancellationToken cancellationToken)
    {
        return await db.ApiClients.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
    }
}
