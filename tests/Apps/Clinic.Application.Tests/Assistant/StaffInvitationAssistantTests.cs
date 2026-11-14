using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Clinic.Agents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Clinic.Infrastructure.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class StaffInvitationAssistantTests
{
    private static readonly IDataProtectionProvider Protection = new EphemeralDataProtectionProvider();

    private const string StaffRole = "TenantUser";
    private const string RoleArgument = "role";
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2030-01-01T12:00:00Z");

    [Theory]
    [InlineData(AssistantOperationsScope.TenantOwnerRole, true)]
    [InlineData(AssistantOperationsScope.PlatformAdminRole, true)]
    [InlineData(StaffRole, false)]
    public void Capability_and_action_are_available_only_to_owners_and_platform_admins(string role, bool expected)
    {
        StaffAssistantCapability capability = StaffCapability();
        AssistantContext context = PracticeContext(role);
        AssistantCapabilityRegistry registry = Registry(capability, new StaffInvitationAssistantActionCommitter(new Mock<ISender>().Object));

        registry.GetAvailable(context).Select(item => item.Name).Contains(AssistantStaffInvitation.CapabilityKey)
            .Should().Be(expected);
        registry.ResolveAction(AssistantStaffInvitation.ActionType)!.Authorizes(context).Should().Be(expected);
    }

    [Fact]
    public void Capability_is_not_available_outside_the_practice_subject()
    {
        StaffAssistantCapability capability = StaffCapability();

        capability.IsAvailable(PracticeContext(AssistantOperationsScope.TenantOwnerRole) with { SubjectKind = AssistantSubjects.PatientKind })
            .Should().BeFalse();
    }

    [Fact]
    public void Capability_belongs_to_the_operations_profile_only()
    {
        AssistantCapabilityKeys.OperationsProfile.Should().Contain(AssistantCapabilityKeys.Staff);
        AssistantCapabilityKeys.Patient.Should().NotContain(AssistantCapabilityKeys.Staff);
        AssistantCapabilityKeys.Staff.Should().Be(StaffCapability().Name);
    }

    [Fact]
    public void Policy_is_prepared_for_owners_and_retryable()
    {
        AssistantActionPolicy policy = StaffCapability().ActionPolicies.Single(item => item.ActionType == AssistantStaffInvitation.ActionType);

        policy.ActionType.Should().Be(AssistantStaffInvitation.ActionType);
        policy.OwningWorkflow.Should().Be(AssistantStaffInvitation.WorkflowKey);
        policy.ExecutionMode.Should().Be(AssistantActionExecutionMode.PrepareAndConfirm);
        policy.RequiredRoles.Should().BeEquivalentTo([AssistantOperationsScope.TenantOwnerRole]);
        policy.RetryClassification.Should().Be(AssistantActionRetryClassification.Retryable);
        policy.ToolNames.Should().BeEquivalentTo([AssistantStaffInvitation.PrepareToolName]);
        // The owner may edit an invitation before confirming; the edit runs the tool's validation again.
        policy.Revisable.Should().BeTrue();
    }

    [Fact]
    public void Registry_validation_accepts_the_policy_tool_and_committer_together()
    {
        Action compose = () => Registry(
            StaffCapability(),
            new StaffInvitationAssistantActionCommitter(new Mock<ISender>().Object));

        compose.Should().NotThrow();
        Registry(
            StaffCapability(),
            new StaffInvitationAssistantActionCommitter(new Mock<ISender>().Object))
            .ResolveTool(AssistantStaffInvitation.PrepareToolName)!.ActionType.Should().Be(AssistantStaffInvitation.ActionType);
    }

    [Fact]
    public void Registry_validation_requires_the_committer_for_the_prepared_action()
    {
        Action compose = () => Registry(StaffCapability());

        compose.Should().Throw<InvalidOperationException>().WithMessage("*has no committer*");
    }

    [Fact]
    public async Task Staff_member_cannot_confirm_an_invitation()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        StaffInvitationAssistantActionCommitter committer = new(sender.Object);
        AssistantCapabilityRegistry registry = Registry(StaffCapability(), committer);
        InMemoryActionStore store = new(PreparedInvitation());
        ConfirmAssistantActionHandler handler = new(
            new PreparedActionLifecycle(store, new FixedTimeProvider(Now)),
            new AssistantActionCommitterRegistry([committer]),
            registry);

        AssistantActionResult result = await handler.Handle(
            new(store.Current.ActionId, PracticeContext(StaffRole)), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.PermissionDenied);
        store.Current.Status.Should().Be(PreparedActionStatus.Proposed);
        sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(AssistantStaffManagement.RemoveActionType)]
    [InlineData(Vitarish.Clinic.Application.Setup.ClinicSetupAssistant.ProfileActionType)]
    public async Task Staff_member_cannot_confirm_a_removal_or_a_profile_change(string actionType)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        IAssistantActionCommitter[] committers =
        [
            new StaffInvitationAssistantActionCommitter(sender.Object),
            new StaffRemovalAssistantActionCommitter(sender.Object),
            new ClinicProfileAssistantActionCommitter(sender.Object)
        ];
        AssistantCapabilityRegistry registry = new(
            [StaffCapability(), new Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup.SetupAssistantCapability(
                new Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup.GetClinicSetupProgressTool(sender.Object),
                new Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup.PrepareUpdateClinicProfileTool(sender.Object))],
            new AssistantAuthorizationPolicy(), new AssistantToolAvailabilityPolicy(), committers);
        InMemoryActionStore store = new(PreparedInvitation() with
        {
            ActionType = actionType,
            ReviewPayload = actionType == AssistantStaffManagement.RemoveActionType
                ? """{"userId":"staff-1","name":"Sara Karimi","contact":""}"""
                : """{"website":"https://clinic.example.test"}"""
        });
        ConfirmAssistantActionHandler handler = new(
            new PreparedActionLifecycle(store, new FixedTimeProvider(Now)),
            new AssistantActionCommitterRegistry(committers),
            registry);

        AssistantActionResult result = await handler.Handle(
            new(store.Current.ActionId, PracticeContext(StaffRole)), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.PermissionDenied);
        store.Current.Status.Should().Be(PreparedActionStatus.Proposed);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Owner_confirmation_sends_the_command_and_completes_with_the_new_user()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(
                It.Is<ProvisionTenantMemberCommand>(command =>
                    command.UserId == null && command.PhoneNumber == "+989121234567" && command.Email == null &&
                    command.FirstName == "Sara" && command.LastName == "Karimi"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProvisionTenantMemberResult(new User { Id = "user-9" }, "TenantUser", true, false));
        StaffInvitationAssistantActionCommitter committer = new(sender.Object);
        AssistantCapabilityRegistry registry = Registry(StaffCapability(), committer);
        InMemoryActionStore store = new(PreparedInvitation());
        ConfirmAssistantActionHandler handler = new(
            new PreparedActionLifecycle(store, new FixedTimeProvider(Now)),
            new AssistantActionCommitterRegistry([committer]),
            registry);

        AssistantActionResult result = await handler.Handle(
            new(store.Current.ActionId, PracticeContext(AssistantOperationsScope.TenantOwnerRole)), CancellationToken.None);

        result.Outcome.Should().Be(AssistantActionOutcome.Succeeded);
        result.Action!.Status.Should().Be(Vitarish.Clinic.Application.Assistant.AssistantActionStatus.Completed);
        sender.VerifyAll();
    }

    [Fact]
    public async Task Created_completes_with_the_new_user_id()
    {
        AssistantActionCommitResult result = await Commit(new ProvisionTenantMemberResult(new User { Id = "user-9" }, "TenantUser", true, false));

        result.Succeeded.Should().BeTrue();
        JsonElement payload = JsonDocument.Parse(result.CompletionPayload!).RootElement;
        payload.GetProperty("userId").GetString().Should().Be("user-9");
        payload.GetProperty("created").GetBoolean().Should().BeTrue();
        payload.GetProperty("alreadyMember").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Already_a_member_completes_and_says_so()
    {
        AssistantActionCommitResult result = await Commit(new ProvisionTenantMemberResult(new User { Id = "user-9" }, "TenantUser", false, true));

        result.Succeeded.Should().BeTrue();
        JsonElement payload = JsonDocument.Parse(result.CompletionPayload!).RootElement;
        payload.GetProperty("userId").GetString().Should().Be("user-9");
        payload.GetProperty("alreadyMember").GetBoolean().Should().BeTrue();
    }

    [Theory]
    [InlineData(UsersCommandErrorCodes.InvalidContact, AssistantStaffInvitation.FailureCodes.InvalidContact, false)]
    [InlineData(UsersCommandErrorCodes.ActiveSessionConflict, AssistantStaffInvitation.FailureCodes.ActiveSessionConflict, true)]
    [InlineData(UsersCommandErrorCodes.Unauthorized, AssistantStaffInvitation.FailureCodes.PermissionDenied, false)]
    [InlineData(UsersCommandErrorCodes.ContactIdentifierInUse, AssistantStaffInvitation.FailureCodes.ContactInUse, false)]
    [InlineData(UsersCommandErrorCodes.RoleScopeConflict, AssistantStaffInvitation.FailureCodes.RoleScopeConflict, false)]
    [InlineData(UsersCommandErrorCodes.TenantMemberConflict, AssistantStaffInvitation.FailureCodes.MembershipConflict, true)]
    [InlineData(UsersCommandErrorCodes.RoleNotFound, AssistantStaffInvitation.FailureCodes.InvitationFailed, false)]
    public async Task Command_failures_map_to_their_failure_and_retry_classification(string errorCode, string code, bool retryable)
    {
        AssistantActionCommitResult result = await Commit(new ProvisionTenantMemberResult(null, "TenantUser", false, false, errorCode));

        result.Succeeded.Should().BeFalse();
        result.Failure!.Code.Should().Be(code);
        result.Failure.Retryable.Should().Be(retryable);
    }

    [Fact]
    public async Task Invalid_review_fails_without_sending_the_command()
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        StaffInvitationAssistantActionCommitter committer = new(sender.Object);

        AssistantActionCommitResult result = await committer.CommitAsync(
            ClinicPreparedActionMapping.ToClinic(PreparedInvitation(reviewJson: "not json")),
            PracticeContext(AssistantOperationsScope.TenantOwnerRole),
            CancellationToken.None);

        result.Failure!.Code.Should().Be(AssistantStaffInvitation.FailureCodes.InvalidReview);
        result.Failure.Retryable.Should().BeFalse();
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public void Tool_schema_has_no_role_and_allows_no_other_property()
    {
        using JsonDocument schema = JsonDocument.Parse(new PrepareInviteMemberTool(Protection).Definition.ParametersJsonSchema);
        JsonElement root = schema.RootElement;

        root.GetProperty("properties").EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["phoneNumber", "email", "firstName", "lastName"]);
        root.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        root.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .Should().BeEquivalentTo(["firstName", "lastName"]);
    }

    [Theory]
    [InlineData("""{"phoneNumber":"09121234567","email":"a@b.com","firstName":"Sara","lastName":"Karimi"}""")]
    [InlineData("""{"firstName":"Sara","lastName":"Karimi"}""")]
    [InlineData("""{"phoneNumber":"123","firstName":"Sara","lastName":"Karimi"}""")]
    [InlineData("""{"email":"not-an-email","firstName":"Sara","lastName":"Karimi"}""")]
    [InlineData("""{"email":"a@b.com","firstName":"","lastName":"Karimi"}""")]
    [InlineData("""{"email":"a@b.com","firstName":"Sara","lastName":"Karimi","role":"TenantOwner"}""")]
    [InlineData("not json")]
    public async Task Tool_rejects_invalid_input_without_preparing(string arguments)
    {
        PrepareInviteMemberTool tool = new(Protection);

        AgentToolResult result = await tool.ExecuteAsync(new("call-1", AssistantStaffInvitation.PrepareToolName, arguments), Execution());

        result.IsError.Should().BeTrue();
        tool.GetPreparedAction(result).Should().BeNull();
    }

    [Fact]
    public async Task Tool_rejects_a_role_argument_even_when_the_rest_is_valid()
    {
        PrepareInviteMemberTool tool = new(Protection);
        string arguments = $$"""{"email":"a@b.com","firstName":"Sara","lastName":"Karimi","{{RoleArgument}}":"TenantOwner"}""";

        AgentToolResult result = await tool.ExecuteAsync(new("call-1", AssistantStaffInvitation.PrepareToolName, arguments), Execution());

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Model_sees_a_masked_contact_while_the_review_holds_the_full_one()
    {
        CapturingLogger<PrepareInviteMemberTool> logger = new();
        PrepareInviteMemberTool tool = new(Protection, logger);

        AgentToolResult phone = await tool.ExecuteAsync(new("call-1", AssistantStaffInvitation.PrepareToolName,
            """{"phoneNumber":"0912 123 4567","firstName":" Sara ","lastName":"Karimi"}"""), Execution());
        AgentToolResult email = await tool.ExecuteAsync(new("call-2", AssistantStaffInvitation.PrepareToolName,
            """{"email":"sara.karimi@example.com","firstName":"Sara","lastName":"Karimi"}"""), Execution());

        phone.IsError.Should().BeFalse();
        MaskedContact(phone).Should().Be("+98•••••4567");
        phone.Output.Should().NotContain("9121234567").And.NotContain("0912");
        MaskedContact(email).Should().Be("s•••@example.com");
        email.Output.Should().NotContain("sara.karimi").And.NotContain("karimi@");
        logger.Messages.Should().NotBeEmpty();
        logger.Messages.Should().OnlyContain(message => !message.Contains("9121234567") && !message.Contains("sara.karimi"));

        AssistantPreparedAction phoneAction = tool.GetPreparedAction(phone)!;
        AssistantPreparedAction emailAction = tool.GetPreparedAction(email)!;
        phoneAction.ActionType.Should().Be(AssistantStaffInvitation.ActionType);
        phoneAction.Capability.Should().Be(AssistantStaffInvitation.CapabilityKey);
        phoneAction.OwningId.Should().NotBe(Guid.Empty).And.NotBe(emailAction.OwningId);
        JsonElement phoneReview = JsonDocument.Parse(phoneAction.ReviewDataJson).RootElement;
        phoneReview.GetProperty("phoneNumber").GetString().Should().Be("+989121234567");
        phoneReview.GetProperty("firstName").GetString().Should().Be("Sara");
        phoneReview.TryGetProperty("email", out _).Should().BeFalse();
        phoneReview.EnumerateObject().Select(property => property.Name).Should().NotContain(RoleArgument);
        JsonDocument.Parse(emailAction.ReviewDataJson).RootElement.GetProperty("email").GetString()
            .Should().Be("sara.karimi@example.com");
        using JsonDocument output = JsonDocument.Parse(phone.Output);
        output.RootElement.GetProperty("actionId").GetGuid().Should().Be(phoneAction.OwningId);
    }

    [Fact]
    public async Task A_review_prepared_in_the_callback_request_is_collected_by_another_tool_instance()
    {
        // Production runs the tool in the runtime callback request and collects the prepared action in the
        // conversation request, each with its own tool instance; only the shared key ring connects them.
        PrepareInviteMemberTool callbackTool = new(Protection);
        PrepareInviteMemberTool conversationTool = new(Protection);

        AgentToolResult result = await callbackTool.ExecuteAsync(new("call-1", AssistantStaffInvitation.PrepareToolName,
            """{"email":"sara.karimi@example.com","firstName":"Sara","lastName":"Karimi"}"""), Execution());

        AssistantPreparedAction prepared = conversationTool.GetPreparedAction(result)!;
        prepared.Should().NotBeNull();
        JsonDocument.Parse(prepared.ReviewDataJson).RootElement.GetProperty("email").GetString().Should().Be("sara.karimi@example.com");
    }

    [Fact]
    public async Task A_tampered_or_reattached_sealed_review_prepares_nothing()
    {
        PrepareInviteMemberTool tool = new(Protection);
        AgentToolResult result = await tool.ExecuteAsync(new("call-1", AssistantStaffInvitation.PrepareToolName,
            """{"email":"sara.karimi@example.com","firstName":"Sara","lastName":"Karimi"}"""), Execution());
        System.Text.Json.Nodes.JsonObject output = System.Text.Json.Nodes.JsonNode.Parse(result.Output)!.AsObject();

        System.Text.Json.Nodes.JsonObject reattached = output.DeepClone().AsObject();
        reattached["actionId"] = Guid.NewGuid();
        System.Text.Json.Nodes.JsonObject tampered = output.DeepClone().AsObject();
        tampered["sealedReview"] = "not-a-sealed-review";
        PrepareInviteMemberTool otherKeys = new(new EphemeralDataProtectionProvider());

        tool.GetPreparedAction(new(result.CallId, reattached.ToJsonString())).Should().BeNull();
        tool.GetPreparedAction(new(result.CallId, tampered.ToJsonString())).Should().BeNull();
        otherKeys.GetPreparedAction(result).Should().BeNull();
    }

    [Fact]
    public async Task Commit_logs_only_the_masked_contact()
    {
        CapturingLogger<StaffInvitationAssistantActionCommitter> logger = new();
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<ProvisionTenantMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProvisionTenantMemberResult(new User { Id = "user-9" }, "TenantUser", true, false));
        StaffInvitationAssistantActionCommitter committer = new(sender.Object, logger);

        _ = await committer.CommitAsync(
            ClinicPreparedActionMapping.ToClinic(PreparedInvitation()),
            PracticeContext(AssistantOperationsScope.TenantOwnerRole),
            CancellationToken.None);

        logger.Messages.Should().ContainSingle().Which.Should()
            .Contain(AssistantContactMasking.Phone("+989121234567")).And.NotContain("9121234567");
    }

    [Theory]
    [InlineData("+989121234567", "+98•••••4567")]
    [InlineData("123", "•••••")]
    public void Phone_masking_keeps_only_the_prefix_and_last_four_digits(string phone, string expected) =>
        AssistantContactMasking.Phone(phone).Should().Be(expected);

    [Theory]
    [InlineData("sara@example.com", "s•••@example.com")]
    [InlineData("nodomain", "•••")]
    public void Email_masking_keeps_only_the_first_letter_and_domain(string email, string expected) =>
        AssistantContactMasking.Email(email).Should().Be(expected);

    private static async Task<AssistantActionCommitResult> Commit(ProvisionTenantMemberResult outcome)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<ProvisionTenantMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);
        return await new StaffInvitationAssistantActionCommitter(sender.Object).CommitAsync(
            ClinicPreparedActionMapping.ToClinic(PreparedInvitation()),
            PracticeContext(AssistantOperationsScope.TenantOwnerRole),
            CancellationToken.None);
    }

    /// <summary>A registry for the staff capability. With any committer given, the removal committer is added too.</summary>
    private static AssistantCapabilityRegistry Registry(
        StaffAssistantCapability capability,
        params IAssistantActionCommitter[] committers) =>
        new([capability], new AssistantAuthorizationPolicy(), new AssistantToolAvailabilityPolicy(),
            committers.Length == 0 || committers.Any(item => item.ActionType == AssistantStaffManagement.RemoveActionType)
                ? committers
                : [.. committers, new StaffRemovalAssistantActionCommitter(new Mock<ISender>().Object)]);

    private static StaffAssistantCapability StaffCapability()
    {
        Mock<ISender> sender = new();
        return new(new PrepareInviteMemberTool(Protection), new ListStaffMembersTool(sender.Object), new PrepareRemoveMemberTool(sender.Object));
    }

    private static AgentExecutionContext Execution() => new("run-1");

    private static string MaskedContact(AgentToolResult result) =>
        JsonDocument.Parse(result.Output).RootElement.GetProperty("maskedContact").GetString()!;

    private static AssistantContext PracticeContext(string role) =>
        new("practice-1", Guid.Empty, "user-1", new HashSet<string>(StringComparer.Ordinal), role,
            "en-US", "UTC", "conversation-1", SubjectKind: AssistantSubjects.PracticeKind);

    private static PreparedAction PreparedInvitation(string? reviewJson = null) => new(
        Guid.NewGuid(),
        AssistantSubjects.OperationsAgentKey,
        "practice-1",
        AssistantSubjects.PracticeKind,
        "practice-1",
        "conversation-1",
        "run-1",
        "user-1",
        AssistantStaffInvitation.CapabilityKey,
        AssistantStaffInvitation.ActionType,
        1,
        reviewJson ?? """{"phoneNumber":"+989121234567","firstName":"Sara","lastName":"Karimi"}""",
        "Invite Sara Karimi",
        "call-1",
        AssistantStaffInvitation.PrepareToolName,
        PreparedActionStatus.Proposed,
        Now.AddMinutes(-1),
        Now.AddMinutes(30));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private sealed class InMemoryActionStore(PreparedAction seed) : IPreparedActionStore
    {
        private readonly object _sync = new();
        private PreparedAction _current = seed;
        private long _version;

        public PreparedAction Current
        {
            get { lock (_sync) return _current; }
        }

        public Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken) =>
            throw new NotSupportedException("The tests start from a stored action.");

        public Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
        {
            lock (_sync)
                return Task.FromResult(_current.ActionId == actionId
                    ? new StoredPreparedAction(_current, _version.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    : null);
        }

        public Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_current.ActionId != next.ActionId ||
                    current.Version != _version.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    return Task.FromResult(false);
                _current = next;
                _version++;
                return Task.FromResult(true);
            }
        }
    }
}
