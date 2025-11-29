using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;

namespace Vorn.Aaas.AuthenticationClients.Application.Queries;

/// <summary>
/// Query for all authentication clients.
/// </summary>
public sealed record GetAuthenticationClients : IRequest<IEnumerable<AuthenticationClient>>;

/// <summary>
/// Validator for <see cref="GetAuthenticationClients"/>.
/// </summary>
public sealed class GetAuthenticationClientsValidator : AbstractValidator<GetAuthenticationClients> { }

internal sealed class GetAuthenticationClientsHandler(AuthenticationClientDbContext db) : IRequestHandler<GetAuthenticationClients, IEnumerable<AuthenticationClient>>
{
    public async Task<IEnumerable<AuthenticationClient>> Handle(GetAuthenticationClients request, CancellationToken cancellationToken)
        => await db.AuthenticationClients.AsNoTracking().ToListAsync(cancellationToken);
}
