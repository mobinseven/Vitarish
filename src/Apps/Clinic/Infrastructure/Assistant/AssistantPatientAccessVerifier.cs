using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Clinic.Infrastructure.Assistant;

public sealed class AssistantPatientAccessVerifier(IUsersService usersService) : IAssistantPatientAccessVerifier
{
    private const string AccessClaimType = "access";

    public async Task<AssistantPatientAccess> RevalidateAsync(
        string practiceId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(practiceId) || string.IsNullOrWhiteSpace(userId))
            return new(new HashSet<string>(StringComparer.Ordinal), string.Empty);

        IList<System.Security.Claims.Claim> claims = await usersService.GetUserClaimsAsync(userId, cancellationToken);
        IReadOnlySet<string> permissions = claims
            .Where(claim => string.Equals(claim.Type, AccessClaimType, StringComparison.Ordinal))
            .Select(claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        bool platformAdmin = await usersService.CheckUserRoleAsync(
            userId, AssistantOperationsScope.PlatformAdminRole, cancellationToken);
        return new(permissions, platformAdmin ? AssistantOperationsScope.PlatformAdminRole : string.Empty);
    }
}
