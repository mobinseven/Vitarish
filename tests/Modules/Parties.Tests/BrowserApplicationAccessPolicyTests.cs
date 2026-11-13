using FluentAssertions;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Users.Application.Authorization;

namespace Parties.Tests;

public sealed class BrowserApplicationAccessPolicyTests
{
    [Fact]
    public void Project_access_requires_membership_in_the_destination_tenant()
    {
        Guid destination = Guid.NewGuid();
        Guid another = Guid.NewGuid();
        HashSet<string> roles = [TenantRoleDefinitions.BuildRoleName(another, IdentityDefinitions.TenantOwner)];
        BrowserApplicationAccessPolicy.Allows(destination, BrowserApplicationTenants.ProjectManagementAppId, roles, false, false).Should().BeFalse();
        roles.Add(TenantRoleDefinitions.BuildRoleName(destination, IdentityDefinitions.TenantUser));
        BrowserApplicationAccessPolicy.Allows(destination, BrowserApplicationTenants.ProjectManagementAppId, roles, false, false).Should().BeTrue();
    }

    [Fact]
    public void Role_free_admission_never_grants_platform_administration()
    {
        HashSet<string> roles = [];
        Guid target = Guid.NewGuid();
        BrowserApplicationAccessPolicy.Allows(target, PlatformAdminTenant.AppId, roles, true, true).Should().BeFalse();
        roles.Add(IdentityDefinitions.PlatformAdmin);
        BrowserApplicationAccessPolicy.Allows(target, PlatformAdminTenant.AppId, roles, false, false).Should().BeTrue();
        BrowserApplicationAccessPolicy.Allows(target, BrowserApplicationTenants.PatientsAppId, new HashSet<string>(), false, false).Should().BeTrue();
    }

    [Fact]
    public void The_clinic_landing_host_admits_signed_in_people_without_roles()
    {
        BrowserApplicationAccessPolicy.Allows(Guid.NewGuid(), BrowserApplicationTenants.ClinicLandingAppId, new HashSet<string>(), false, false).Should().BeTrue();
    }
}
