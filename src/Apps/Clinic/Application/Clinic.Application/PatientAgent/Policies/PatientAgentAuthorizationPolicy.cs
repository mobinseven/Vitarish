using Vitarish.Clinic.Application.PatientAgent.Capabilities;

namespace Vitarish.Clinic.Application.PatientAgent.Policies;

public sealed class PatientAgentAuthorizationPolicy
{
    public bool IsAuthorized(IPatientAgentCapability capability, PatientAgentContext context) =>
        string.Equals(context.Role, "PlatformAdmin", StringComparison.Ordinal) ||
        capability.RequiredPermissions.All(context.Permissions.Contains);
}
