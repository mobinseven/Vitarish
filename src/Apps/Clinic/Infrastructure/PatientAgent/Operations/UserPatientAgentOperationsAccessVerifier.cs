using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Operations;

public sealed class UserPatientAgentOperationsAccessVerifier(IUsersService usersService)
    : IPatientAgentOperationsAccessVerifier
{
    public async Task<string?> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(practiceId) || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (await usersService.CheckUserRoleAsync(userId, PatientAgentOperationsScope.PlatformAdminRole, cancellationToken))
        {
            return PatientAgentOperationsScope.PlatformAdminRole;
        }

        return await usersService.CheckUserRoleAsync(
            userId,
            $"{practiceId}:{PatientAgentOperationsScope.TenantOwnerRole}",
            cancellationToken)
            ? PatientAgentOperationsScope.TenantOwnerRole
            : null;
    }
}
