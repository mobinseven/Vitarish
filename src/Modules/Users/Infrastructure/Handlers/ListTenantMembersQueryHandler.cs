using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class ListTenantMembersQueryHandler(
    UsersDbContext dbContext,
    IRequestContext requestContext) : IRequestHandler<ListTenantMembersQuery, IReadOnlyList<TenantMemberSummary>>
{
    public async Task<IReadOnlyList<TenantMemberSummary>> Handle(ListTenantMembersQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId) ||
            !UsersSecurityAuthorization.CanManageTenantMembers(requestContext.User, tenantId))
            return [];

        string ownerRoleName = TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner);
        string userRoleName = TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser);
        string search = request.Search?.Trim() ?? string.Empty;

        var rows = await (from membership in dbContext.UserRoles.AsNoTracking()
                          join role in dbContext.Roles.AsNoTracking() on membership.RoleId equals role.Id
                          join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
                          where (role.Name == ownerRoleName || role.Name == userRoleName) &&
                                (search.Length == 0 ||
                                 (user.FirstName != null && user.FirstName.Contains(search)) ||
                                 (user.LastName != null && user.LastName.Contains(search)) ||
                                 (user.UserName != null && user.UserName.Contains(search)) ||
                                 (user.Email != null && user.Email.Contains(search)) ||
                                 (user.PhoneNumber != null && user.PhoneNumber.Contains(search)) ||
                                 user.Id.Contains(search))
                          select new
                          {
                              user.Id,
                              user.UserName,
                              user.FirstName,
                              user.LastName,
                              user.Email,
                              user.PhoneNumber,
                              user.FullName,
                              RoleSuffix = role.Name == ownerRoleName ? IdentityDefinitions.TenantOwner : IdentityDefinitions.TenantUser
                          })
            .Take(request.MaxResults * 2)
            .ToListAsync(cancellationToken);

        return rows.GroupBy(row => row.Id, StringComparer.Ordinal)
            .Select(group =>
            {
                var row = group.First();
                string roleSuffix = group.Any(item => item.RoleSuffix == IdentityDefinitions.TenantOwner)
                    ? IdentityDefinitions.TenantOwner
                    : IdentityDefinitions.TenantUser;
                return new TenantMemberSummary(row.Id, row.UserName, row.FirstName, row.LastName, row.Email, row.PhoneNumber, row.FullName, roleSuffix);
            })
            .Take(request.MaxResults)
            .ToArray();
    }
}
