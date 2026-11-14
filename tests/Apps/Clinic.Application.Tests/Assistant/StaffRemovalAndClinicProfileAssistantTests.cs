using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Clinic.Infrastructure.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.PreparedActions;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Contracts.Responses;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class StaffRemovalAndClinicProfileAssistantTests
{
    private static readonly AgentExecutionContext Execution = new("run-1");
    private static readonly TenantMemberSummary Staff = new("staff-1", "staff", "Sara", "Karimi", null, "+989121234567", null, IdentityDefinitions.TenantUser);
    private static readonly TenantMemberSummary Owner = new("owner-1", "owner", "Omid", "Rad", "owner@example.test", null, null, IdentityDefinitions.TenantOwner);

    [Fact]
    public async Task Staff_list_masks_contacts_and_labels_roles()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.Is<ListTenantMembersQuery>(query => query.Search == "Sa"), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Owner, Staff]);

        AgentToolResult result = await new ListStaffMembersTool(sender.Object)
            .ExecuteAsync(new("call-1", AssistantStaffManagement.ListToolName, """{"search":"Sa"}"""), Execution);

        result.Output.Should().NotContain("9121234567").And.NotContain("owner@example.test");
        JsonElement[] members = JsonDocument.Parse(result.Output).RootElement.GetProperty("members").EnumerateArray().ToArray();
        members.Select(member => member.GetProperty("role").GetString()).Should().Equal(StaffMemberView.OwnerRole, StaffMemberView.StaffRole);
        members[1].GetProperty("name").GetString().Should().Be("Sara Karimi");
    }

    [Fact]
    public async Task Removal_cannot_be_prepared_for_an_owner()
    {
        AgentToolResult result = await new PrepareRemoveMemberTool(MemberLookup(Owner).Object)
            .ExecuteAsync(new("call-1", AssistantStaffManagement.PrepareRemoveToolName, """{"userId":"owner-1"}"""), Execution);

        result.IsError.Should().BeTrue();
        new PrepareRemoveMemberTool(new Mock<ISender>().Object).GetPreparedAction(result).Should().BeNull();
    }

    [Fact]
    public async Task Removal_review_names_the_member_as_the_server_found_them()
    {
        PrepareRemoveMemberTool tool = new(MemberLookup(Staff).Object);

        AgentToolResult result = await tool.ExecuteAsync(new("call-1", AssistantStaffManagement.PrepareRemoveToolName, """{"userId":"staff-1"}"""), Execution);
        AssistantPreparedAction? action = tool.GetPreparedAction(result);

        action.Should().NotBeNull();
        action!.ActionType.Should().Be(AssistantStaffManagement.RemoveActionType);
        JsonElement review = JsonDocument.Parse(action.ReviewDataJson).RootElement;
        review.GetProperty("userId").GetString().Should().Be("staff-1");
        review.GetProperty("name").GetString().Should().Be("Sara Karimi");
        review.GetProperty("contact").GetString().Should().Be(AssistantContactMasking.Phone(Staff.PhoneNumber));
    }

    [Fact]
    public async Task Removal_commit_refuses_a_review_whose_member_no_longer_matches()
    {
        Mock<ISender> sender = MemberLookup(Staff with { FirstName = "Someone" });

        AssistantActionCommitResult result = await new StaffRemovalAssistantActionCommitter(sender.Object)
            .CommitAsync(RemovalAction(), OwnerContext(), CancellationToken.None);

        result.Failure!.Code.Should().Be(AssistantStaffManagement.FailureCodes.MemberChanged);
        sender.Verify(item => item.Send(It.IsAny<RemoveTenantMemberCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Removal_commit_sends_the_users_command_for_the_reviewed_member()
    {
        Mock<ISender> sender = MemberLookup(Staff);
        sender.Setup(item => item.Send(It.Is<RemoveTenantMemberCommand>(command => command.UserId == "staff-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        AssistantActionCommitResult result = await new StaffRemovalAssistantActionCommitter(sender.Object)
            .CommitAsync(RemovalAction(), OwnerContext(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        sender.VerifyAll();
    }

    [Fact]
    public async Task Removal_of_a_signed_in_member_fails_as_retryable()
    {
        Mock<ISender> sender = MemberLookup(Staff);
        sender.Setup(item => item.Send(It.IsAny<RemoveTenantMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = UsersCommandErrorCodes.ActiveSessionConflict }));

        AssistantActionCommitResult result = await new StaffRemovalAssistantActionCommitter(sender.Object)
            .CommitAsync(RemovalAction(), OwnerContext(), CancellationToken.None);

        result.Failure!.Code.Should().Be(AssistantStaffManagement.FailureCodes.ActiveSessionConflict);
        result.Failure.Retryable.Should().BeTrue();
    }

    [Fact]
    public void Removal_and_profile_changes_are_prepared_for_owners_only()
    {
        Mock<ISender> sender = new();
        AssistantActionPolicy removal = new StaffAssistantCapability(
                new PrepareInviteMemberTool(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider()),
                new ListStaffMembersTool(sender.Object), new PrepareRemoveMemberTool(sender.Object))
            .ActionPolicies.Single(policy => policy.ActionType == AssistantStaffManagement.RemoveActionType);
        AssistantActionPolicy profile = new SetupAssistantCapability(new GetClinicSetupProgressTool(sender.Object), new PrepareUpdateClinicProfileTool(sender.Object))
            .ActionPolicies.Single();

        foreach (AssistantActionPolicy policy in new[] { removal, profile })
        {
            policy.ExecutionMode.Should().Be(AssistantActionExecutionMode.PrepareAndConfirm);
            policy.RequiredRoles.Should().BeEquivalentTo([AssistantOperationsScope.TenantOwnerRole]);
        }
        profile.ActionType.Should().Be(ClinicSetupAssistant.ProfileActionType);
    }

    [Fact]
    public async Task Profile_change_needs_at_least_one_part_and_valid_values()
    {
        PrepareUpdateClinicProfileTool tool = new(ProfileLookup(Profile()).Object);

        AgentToolResult empty = await tool.ExecuteAsync(new("call-1", ClinicSetupAssistant.PrepareProfileToolName, "{}"), Execution);
        AgentToolResult badWebsite = await tool.ExecuteAsync(new("call-2", ClinicSetupAssistant.PrepareProfileToolName, """{"website":"not a url"}"""), Execution);

        empty.IsError.Should().BeTrue();
        badWebsite.IsError.Should().BeTrue();
        badWebsite.Output.Should().Contain("website");
    }

    [Fact]
    public async Task Profile_review_holds_only_the_parts_that_change()
    {
        PrepareUpdateClinicProfileTool tool = new(ProfileLookup(Profile()).Object);

        AgentToolResult result = await tool.ExecuteAsync(
            new("call-1", ClinicSetupAssistant.PrepareProfileToolName, """{"address":{"city":"Tehran","street":"Valiasr 10"}}"""), Execution);
        AssistantPreparedAction? action = tool.GetPreparedAction(result);

        action.Should().NotBeNull();
        JsonElement review = JsonDocument.Parse(action!.ReviewDataJson).RootElement;
        review.GetProperty("address").GetProperty("city").GetString().Should().Be("Tehran");
        review.TryGetProperty("phones", out _).Should().BeFalse();
        review.TryGetProperty("description", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Profile_commit_keeps_hours_photos_pin_and_category_it_does_not_change()
    {
        TenantBusinessProfileView current = Profile();
        Mock<ISender> sender = ProfileLookup(current);
        UpdateTenantBusinessProfileCommand? sent = null;
        sender.Setup(item => item.Send(It.IsAny<UpdateTenantBusinessProfileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TenantBusinessProfileUpdateResult>, CancellationToken>((command, _) => sent = (UpdateTenantBusinessProfileCommand)command)
            .ReturnsAsync(new TenantBusinessProfileUpdateResult(TenantBusinessProfileUpdateStatus.Updated));

        AssistantActionCommitResult result = await new ClinicProfileAssistantActionCommitter(sender.Object).CommitAsync(
            ProfileAction("""{"address":{"city":"Tehran","street":"Valiasr 10"},"phones":["021 1234 5678"]}"""), OwnerContext(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        sent!.Category.Should().Be(current.Category);
        sent.Description.Should().Be(current.Description);
        sent.Details!.Address!.City.Should().Be("Tehran");
        sent.Details.Address.Latitude.Should().Be(35.7);
        sent.Details.Phones.Should().Equal("021 1234 5678");
        sent.Details.Website.Should().Be(current.Details!.Website);
        sent.Details.WorkingHours.Should().BeEquivalentTo(current.Details.WorkingHours);
        sent.Details.PhotoFileIds.Should().Equal(current.Details.PhotoFileIds);
    }

    private static Mock<ISender> MemberLookup(TenantMemberSummary member)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(
                It.Is<ListTenantMembersQuery>(query => query.UserIds != null && query.UserIds.Contains(member.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([member]);
        return sender;
    }

    private static Mock<ISender> ProfileLookup(TenantBusinessProfileView profile)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.IsAny<GetTenantBusinessProfileQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        return sender;
    }

    private static TenantBusinessProfileView Profile()
    {
        TenantBusinessDetails? details = TenantBusinessDetails.TryCreate(
            new TenantBusinessAddress("Tehran", "Karaj", "Old street", null, 35.7, 51.4),
            ["021 0000 0000"],
            "https://clinic.example.test",
            null,
            [new TenantWorkingDay(DayOfWeek.Saturday, [new TenantOpeningInterval(new TimeOnly(9, 0), new TimeOnly(13, 0))])],
            [Guid.Parse("44444444-4444-4444-4444-444444444444")],
            out string? error);
        error.Should().BeNull();
        return new("Clinic", "A family clinic", null, null, details, false);
    }

    private static AssistantAction RemovalAction() => Action(
        AssistantStaffInvitation.CapabilityKey, AssistantStaffManagement.RemoveActionType, AssistantStaffManagement.PrepareRemoveToolName,
        """{"userId":"staff-1","name":"Sara Karimi","contact":"+98•••••4567"}""");

    private static AssistantAction ProfileAction(string review) => Action(
        ClinicSetupAssistant.CapabilityName, ClinicSetupAssistant.ProfileActionType, ClinicSetupAssistant.PrepareProfileToolName, review);

    private static AssistantAction Action(string capability, string actionType, string toolName, string review) =>
        ClinicPreparedActionMapping.ToClinic(new PreparedAction(
            Guid.NewGuid(), AssistantSubjects.OperationsAgentKey, "practice-1", AssistantSubjects.PracticeKind, "practice-1",
            "conversation-1", "run-1", "owner-1", capability, actionType, 1, review, "Review", "call-1", toolName,
            PreparedActionStatus.Confirming, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(30)));

    private static AssistantContext OwnerContext() =>
        new("practice-1", Guid.Empty, "owner-1", new HashSet<string>(StringComparer.Ordinal), AssistantOperationsScope.TenantOwnerRole,
            "en-US", "UTC", "conversation-1", SubjectKind: AssistantSubjects.PracticeKind);
}
