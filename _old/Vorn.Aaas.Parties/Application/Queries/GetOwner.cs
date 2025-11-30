using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;
/// <summary>
/// Query for a single <see cref="Owner"/> by identifier.
/// </summary>
public sealed record GetOwner(Guid Id) : IRequest<Owner?>;

internal sealed class GetOwnerHandler(PartiesDbContext db) : IRequestHandler<GetOwner, Owner?>
{
    public async Task<Owner?> Handle(GetOwner request, CancellationToken cancellationToken) =>
        await db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
}
