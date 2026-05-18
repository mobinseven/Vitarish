using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class CreateTenantRolesCommandHandler(UsersDbContext dbContext)
        : IRequestHandler<CreateTenantRolesCommand>
{
    public async Task Handle(CreateTenantRolesCommand request, CancellationToken cancellationToken)
    {
        string[] tenantRoles =
        [
            $"{request.TenantId}:{IdentityDefinitions.TenantOwner}",
            $"{request.TenantId}:{IdentityDefinitions.TenantUser}"
        ];

        // Fetch existing roles in a single query
        List<string> existingRoles = await dbContext.Roles
            .Where(r => tenantRoles.Contains(r.Name!))
            .Select(r => r.Name!)
            .ToListAsync(cancellationToken);

        var missingRoles = tenantRoles
            .Except(existingRoles)
            .Select(roleName => new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant()
            })
            .ToList();

        if (missingRoles.Count == 0)
        {
            return;
        }

        dbContext.Roles.AddRange(missingRoles);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
