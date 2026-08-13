using System.Security.Claims;
using FluentAssertions;
using Moq;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Actions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;
using Vitarish.Users.Application.Abstractions;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.ClinicWorkspace;

public sealed class ClinicWorkspaceSecurityRegressionTests
{
    [Fact]
    public async Task Patient_callback_permissions_are_loaded_from_current_user_claims()
    {
        Mock<IUsersService> users = new(MockBehavior.Strict);
        users.Setup(service => service.GetUserClaimsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>
            {
                new("access", "Appointments.Read"),
                new("access", "Appointments.Create"),
                new("tenant", "practice-1")
            });
        users.Setup(service => service.CheckUserRoleAsync(
                "user-1", ClinicWorkspaceOperationsScope.PlatformAdminRole, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ClinicWorkspacePatientAccess access = await new ClinicWorkspacePatientAccessVerifier(users.Object)
            .RevalidateAsync("practice-1", "user-1", CancellationToken.None);

        access.Permissions.Should().BeEquivalentTo(["Appointments.Read", "Appointments.Create"]);
        access.Role.Should().BeEmpty();
        users.VerifyAll();
    }

    [Fact]
    public async Task Patient_callback_revalidation_does_not_retain_platform_admin_after_revocation()
    {
        Mock<IUsersService> users = new(MockBehavior.Strict);
        users.Setup(service => service.GetUserClaimsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());
        users.Setup(service => service.CheckUserRoleAsync(
                "user-1", ClinicWorkspaceOperationsScope.PlatformAdminRole, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ClinicWorkspacePatientAccess access = await new ClinicWorkspacePatientAccessVerifier(users.Object)
            .RevalidateAsync("practice-1", "user-1", CancellationToken.None);

        access.Permissions.Should().BeEmpty();
        access.Role.Should().BeEmpty();
    }

    [Fact]
    public async Task Sea_unknown_commit_outcome_is_not_retryable()
    {
        ClinicWorkspaceAction action = new(
            Guid.NewGuid(), "run-1", "conversation-1", Guid.Empty, "practice-1", "user-1",
            "SEA", "sea_manage", Guid.NewGuid(), "{}", ClinicWorkspaceActionStatus.Proposed,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(10), SubjectKind: "practice");
        Mock<ISeaManagementAgentTool> sea = new(MockBehavior.Strict);
        sea.Setup(tool => tool.ExecutePreparedAsync(
                It.IsAny<AgentToolCall>(), It.IsAny<SeaManagementPreparedAction>(),
                It.IsAny<SeaManagementAgentSubject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentToolResult("call-1", "provider returned no authoritative result", IsError: true));

        ClinicWorkspaceActionCommitResult result = await new SeaManagementClinicWorkspaceActionCommitter(sea.Object)
            .CommitAsync(action, new(
                "practice-1", Guid.Empty, "user-1", new HashSet<string>(), "TenantOwner",
                "en-US", "UTC", "conversation-1", SubjectKind: "practice"), CancellationToken.None);

        result.Failure.Should().BeEquivalentTo(new ClinicWorkspaceActionFailure(
            "unknown_commit_outcome", "The SEA action returned an unknown outcome.", false));
    }
}
