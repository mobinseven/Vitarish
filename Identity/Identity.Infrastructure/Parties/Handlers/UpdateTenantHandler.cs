using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class UpdateTenantHandler(PartiesDbContext db) : IRequestHandler<UpdateTenant, Result>
{
    public async Task<Result> Handle(UpdateTenant request, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", $"Tenant with ID '{request.Id}' was not found."));
        }

        tenant.Update(request.Name, request.ClientId, request.LogoUrl, request.Description, request.ModifiedBy);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
