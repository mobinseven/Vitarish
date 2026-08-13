using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Assistant.Operations;

public sealed class UserAssistantOperationsAccessVerifier(IUsersService usersService)
    : IAssistantOperationsAccessVerifier
{
    public async Task<string?> RevalidateAsync(string practiceId, string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(practiceId) || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (await usersService.CheckUserRoleAsync(userId, AssistantOperationsScope.PlatformAdminRole, cancellationToken))
        {
            return AssistantOperationsScope.PlatformAdminRole;
        }

        return await usersService.CheckUserRoleAsync(
            userId,
            $"{practiceId}:{AssistantOperationsScope.TenantOwnerRole}",
            cancellationToken)
            ? AssistantOperationsScope.TenantOwnerRole
            : null;
    }
}
