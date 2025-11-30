
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;
/// <summary>
/// Query to retrieve all <see cref="Owner"/> entities.
/// </summary>
public sealed record GetOwners : IRequest<IEnumerable<Owner>>;

internal sealed class GetOwnersHandler(PartiesDbContext db) : IRequestHandler<GetOwners, IEnumerable<Owner>>
{
    public async Task<IEnumerable<Owner>> Handle(GetOwners request, CancellationToken cancellationToken) =>
        await db.Owners.AsNoTracking().ToListAsync(cancellationToken);
}
