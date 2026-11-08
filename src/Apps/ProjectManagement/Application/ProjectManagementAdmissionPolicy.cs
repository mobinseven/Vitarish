using Microsoft.Extensions.Options;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.ProjectManagement.Application;

/// <summary>Owns PM's platform-administrator or destination-member admission rule.</summary>
public sealed class ProjectManagementAdmissionPolicy(IOptions<BrowserApplicationHostOptions> hosts) : IAuthenticationApplicationAdmission
{
    public bool? Allows(string origin, Guid tenantId, IReadOnlySet<string> roles) =>
        hosts.Value.ProjectManagementUrl is { } url && new Uri(url).GetLeftPart(UriPartial.Authority) == origin
            ? AllowsTenantRoles(tenantId, roles) : null;

    public static IReadOnlyList<string> AdmissionRoles(Guid tenantId) =>
        [IdentityDefinitions.PlatformAdmin,
         TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner),
         TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser)];

    public static bool AllowsTenantRoles(Guid tenantId, IReadOnlySet<string> roles) =>
        AdmissionRoles(tenantId).Any(roles.Contains);
}
