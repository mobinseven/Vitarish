using Vitarish.Clinic.Application.Assistant.Capabilities;

namespace Vitarish.Clinic.Application.Assistant.Policies;

public sealed class AssistantAuthorizationPolicy
{
    public bool IsAuthorized(IAssistantCapability capability, AssistantContext context) =>
        string.Equals(context.Role, "PlatformAdmin", StringComparison.Ordinal) ||
        capability.RequiredPermissions.All(context.Permissions.Contains);
}
