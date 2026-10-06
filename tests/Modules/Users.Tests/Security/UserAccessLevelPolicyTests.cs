using FluentAssertions;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Users.Tests.Security;

public sealed class UserAccessLevelPolicyTests
{
    [Fact]
    public void A_permitted_global_role_grants_an_access_level()
    {
        UserAccessLevelPolicy.HasAccessLevel([IdentityDefinitions.PlatformAdmin]).Should().BeTrue();
    }

    [Fact]
    public void A_tenant_membership_role_grants_an_access_level()
    {
        string tenantRole = TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner);

        UserAccessLevelPolicy.HasAccessLevel([tenantRole]).Should().BeTrue();
    }

    [Theory]
    [InlineData(IdentityDefinitions.TenantUser)]
    [InlineData("UnrelatedRole")]
    [InlineData("not-a-guid:TenantUser")]
    [InlineData("00000000-0000-0000-0000-000000000001:UnknownSuffix")]
    public void A_role_outside_the_permitted_global_roles_and_tenant_memberships_does_not_grant_access(string roleName)
    {
        UserAccessLevelPolicy.HasAccessLevel([roleName]).Should().BeFalse();
    }

    [Fact]
    public void An_account_without_any_role_has_no_access_level()
    {
        UserAccessLevelPolicy.HasAccessLevel([]).Should().BeFalse();
        UserAccessLevelPolicy.HasAccessLevel([null]).Should().BeFalse();
    }
}
