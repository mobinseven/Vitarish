using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;

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
    public GetAuthenticationClientValidator() => RuleFor(q => q.Id).NotEmpty();
}

internal sealed class GetAuthenticationClientHandler(AuthenticationClientDbContext db) : IRequestHandler<GetAuthenticationClient, AuthenticationClient?>
{
    public async Task<AuthenticationClient?> Handle(GetAuthenticationClient request, CancellationToken cancellationToken)
    {
        return await db.AuthenticationClients.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
    }
}
