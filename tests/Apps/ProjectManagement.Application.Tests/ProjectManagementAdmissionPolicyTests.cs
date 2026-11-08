using FluentAssertions;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application;
using Vitarish.Users.Application.Authorization;
using Xunit;

namespace Vitarish.ProjectManagement.Application.Tests;

public sealed class ProjectManagementAdmissionPolicyTests
{
    [Fact]
    public void Configured_host_uses_PM_admission_even_when_its_operator_tenant_predates_the_app_id()
    {
        var policy = new ProjectManagementAdmissionPolicy(Options.Create(new BrowserApplicationHostOptions { ProjectManagementUrl = "https://pm.example.test" }));
        Guid target = Guid.NewGuid();
        policy.Allows("https://pm.example.test", target, new HashSet<string> { IdentityDefinitions.PlatformAdmin }).Should().BeTrue();
        policy.Allows("https://pm.example.test", target, new HashSet<string>()).Should().BeFalse();
        policy.Allows("https://other.example.test", target, new HashSet<string> { IdentityDefinitions.PlatformAdmin }).Should().BeNull();
        policy.Allows("https://pm.example.test", target, new HashSet<string> { TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner) }).Should().BeFalse();
        policy.Allows("https://pm.example.test", target, new HashSet<string> { TenantRoleDefinitions.BuildRoleName(target, IdentityDefinitions.TenantUser) }).Should().BeTrue();
    }
}
