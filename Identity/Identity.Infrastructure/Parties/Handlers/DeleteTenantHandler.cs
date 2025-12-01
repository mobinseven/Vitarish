using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class DeleteTenantHandler(PartiesDbContext db) : IRequestHandler<DeleteTenant, Result>
{
    public async Task<Result> Handle(DeleteTenant request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{request.Id}' was not found."));
        }

        _ = db.Tenants.Remove(tenant);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
