using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Policies;

public sealed class ClinicWorkspaceAuthorizationPolicy
{
    public bool IsAuthorized(IClinicWorkspaceCapability capability, ClinicWorkspaceContext context) =>
        string.Equals(context.Role, "PlatformAdmin", StringComparison.Ordinal) ||
        capability.RequiredPermissions.All(context.Permissions.Contains);
}
