using FluentAssertions;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Users.Tests.Security;

public sealed class RoleScopeSeparationPolicyTests
{
    private static readonly string ClinicOwnerRole = TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner);
    private static readonly string OtherClinicUserRole = TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantUser);

    [Fact]
    public void A_clinic_member_cannot_be_granted_a_platform_role()
    {
        RoleScopeSeparationPolicy.CanGrantGlobalRole([ClinicOwnerRole]).Should().BeFalse();
    }

    [Fact]
    public void A_platform_administrator_cannot_be_granted_a_clinic_role()
    {
        RoleScopeSeparationPolicy.CanGrantTenantRole([IdentityDefinitions.PlatformAdmin]).Should().BeFalse();
    }

    [Fact]
    public void Clinic_memberships_in_several_clinics_stay_allowed()
    {
        RoleScopeSeparationPolicy.CanGrantTenantRole([ClinicOwnerRole, OtherClinicUserRole]).Should().BeTrue();
    }

    [Fact]
    public void An_account_without_roles_may_receive_either_scope()
    {
        RoleScopeSeparationPolicy.CanGrantGlobalRole([]).Should().BeTrue();
        RoleScopeSeparationPolicy.CanGrantTenantRole([]).Should().BeTrue();
    }

    [Fact]
    public void Unrecognized_role_names_do_not_count_as_a_platform_role()
    {
        RoleScopeSeparationPolicy.CanGrantTenantRole([IdentityDefinitions.PlatformUser]).Should().BeTrue();
    }
}
