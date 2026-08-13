using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Assistant.Operations;

public sealed class UserClinicWorkspaceOperationsAccessVerifier(IUsersService usersService)
    : IClinicWorkspaceOperationsAccessVerifier
{
    public async Task<string?> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(practiceId) || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (await usersService.CheckUserRoleAsync(userId, ClinicWorkspaceOperationsScope.PlatformAdminRole, cancellationToken))
        {
            return ClinicWorkspaceOperationsScope.PlatformAdminRole;
        }

        return await usersService.CheckUserRoleAsync(
            userId,
            $"{practiceId}:{ClinicWorkspaceOperationsScope.TenantOwnerRole}",
            cancellationToken)
            ? ClinicWorkspaceOperationsScope.TenantOwnerRole
            : null;
    }
}
