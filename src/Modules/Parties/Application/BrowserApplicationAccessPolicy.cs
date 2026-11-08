using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Parties.Application;

/// <summary>Owns admission to a registered browser app; app resource policies still apply.</summary>
public static class BrowserApplicationAccessPolicy
{
    public static bool Allows(Guid tenantId, string appId, IReadOnlySet<string> roles, bool roleFree, bool onboardingEnabled)
    {
        if (appId == SignInTenant.AppId || appId == BrowserApplicationTenants.PatientsAppId) return true;
        if (appId == OnboardingTenant.AppId) return onboardingEnabled;
        if (appId == PlatformAdminTenant.AppId) return roles.Contains(IdentityDefinitions.PlatformAdmin);
        return roleFree || roles.Contains(TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner))
            || roles.Contains(TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser));
    }
}
