using FluentAssertions;
using Moq;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class OperationsAgentCapabilityTests
{
    [Fact]
    public async Task Prepared_booking_never_exposes_the_resolved_patient_id()
    {
        Guid patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid proposalId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid shiftId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        AgentSchedulingProposal proposal = new()
        {
            Id = proposalId,
            Operation = AgentSchedulingOperation.BookAppointment,
            Context = new("practice-1", "run-1", "call-1", "prepare_book_appointment", "owner-1"),
            Appointment = new(shiftId, new DateOnly(2030, 1, 2), patientId.ToString("D")),
            ExpiresAt = DateTimeOffset.Parse("2030-01-01T12:10:00Z")
        };
        Mock<IAppointmentSchedulingAgentCapability> scheduling = new(MockBehavior.Strict);
        scheduling.Setup(capability => capability.PrepareBookAppointmentAsync(
                It.IsAny<AgentSchedulingContext>(),
                It.Is<AgentAppointmentInput>(input => input.ReservingPartyId == patientId.ToString("D")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentSchedulingResult<AgentSchedulingProposal>(
                AgentSchedulingStatus.Prepared,
                proposal,
                proposalId));
        Mock<IAssistantPatientSearch> search = new(MockBehavior.Strict);
        search.Setup(service => service.ResolveAsync("practice-1", "owner-1", "run-1", "opaque-candidate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssistantCandidateReference(patientId, "practice-1", "owner-1", "run-1", "Masked patient", "1******9", "0******1"));
        PrepareScheduleActionTool tool = new(scheduling.Object, search.Object, "prepare_book_appointment");
        AgentExecutionContext context = new("run-1", new Dictionary<string, string>
        {
            ["practiceId"] = "practice-1",
            ["authenticatedUserId"] = "owner-1"
        });

        AgentToolResult result = await tool.ExecuteAsync(
            new AgentToolCall("call-1", "prepare_book_appointment", $$"""{"candidateToken":"opaque-candidate","shiftId":"{{shiftId:D}}","date":"2030-01-02"}"""),
            context);
        var action = tool.GetPreparedAction(result);

        result.Output.Should().NotContain(patientId.ToString("D"));
        action.Should().NotBeNull();
        action!.ReviewDataJson.Should().NotContain(patientId.ToString("D"));
        action.OwningId.Should().Be(proposalId);
        scheduling.VerifyAll();
        search.VerifyAll();
    }

    [Fact]
    public void Sea_management_is_available_only_for_practice_operations_scope()
    {
        SeaManagementClinicToolAdapter tool = new(new Mock<ISeaManagementAgentTool>().Object);
        SeaManagementAgentCapability capability = new(tool);
        AssistantContext practice = new(
            "practice-1", Guid.Empty, "owner-1", new HashSet<string>(StringComparer.Ordinal),
            AssistantOperationsScope.TenantOwnerRole,
            "en-US", "UTC", "conversation-1", SubjectKind: "practice");
        AssistantContext patient = practice with { PatientId = Guid.NewGuid(), SubjectKind = "patient" };

        capability.IsAvailable(practice).Should().BeTrue();
        capability.IsAvailable(patient).Should().BeFalse();
    }

    [Fact]
    public async Task Appointment_administration_requires_trusted_confirmation_metadata()
    {
        Guid reservationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        Mock<IAccessControlledReservationManager> reservations = new(MockBehavior.Strict);
        reservations.Setup(x => x.ConfirmReservationAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult.Success());
        AppointmentAdministrationAgentTool tool = new(
            reservations.Object,
            new Mock<IAccessControlledShiftManager>().Object,
            new Mock<IServiceManager>().Object,
            new Mock<IServiceTypeManager>().Object,
            new Mock<IServiceProviderManager>().Object,
            new Mock<IShiftBookingPolicyManager>().Object,
            new Mock<IShiftReminderPolicyManager>().Object,
            new Mock<IShiftBookableDateQueryService>().Object,
            new Mock<ITemporaryHoldManager>().Object);
        AgentToolCall call = new("call-1", "appointment_manage", $$"""{"operation":"confirm_reservation","payload":{"id":"{{reservationId:D}}"},"commit":true}""");

        AgentToolResult untrusted = await tool.ExecuteAsync(call, new AgentExecutionContext("run-1"));
        untrusted.Output.Should().Contain("prepared");
        reservations.Verify(x => x.ConfirmReservationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        AgentToolResult trusted = await tool.ExecuteAsync(call, new AgentExecutionContext("run-1", new Dictionary<string, string>
        {
            ["confirmedAction"] = "true"
        }));
        trusted.Output.Should().Contain("true");
        reservations.Verify(x => x.ConfirmReservationAsync(reservationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("\"Public\"", "\"Monday\"")]
    [InlineData("0", "1")]
    public async Task Appointment_administration_reads_enum_values_by_name_or_number(string visibility, string dayOfWeek)
    {
        Mock<IAccessControlledShiftManager> shifts = new(MockBehavior.Strict);
        Vitarish.Appointments.Domain.Dto.ShiftWriteDto? written = null;
        shifts.Setup(x => x.AddAsync(It.IsAny<Vitarish.Appointments.Domain.Dto.ShiftWriteDto>(), It.IsAny<CancellationToken>()))
            .Callback<Vitarish.Appointments.Domain.Dto.ShiftWriteDto, CancellationToken>((value, _) => written = value)
            .ReturnsAsync(EntityResult<Vitarish.Appointments.Domain.Dto.ShiftReadDto>.Success(new Vitarish.Appointments.Domain.Dto.ShiftReadDto()));
        AppointmentAdministrationAgentTool tool = new(
            new Mock<IAccessControlledReservationManager>().Object,
            shifts.Object,
            new Mock<IServiceManager>().Object,
            new Mock<IServiceTypeManager>().Object,
            new Mock<IServiceProviderManager>().Object,
            new Mock<IShiftBookingPolicyManager>().Object,
            new Mock<IShiftReminderPolicyManager>().Object,
            new Mock<IShiftBookableDateQueryService>().Object,
            new Mock<ITemporaryHoldManager>().Object);
        AgentToolCall call = new("call-1", "appointment_manage",
            $$"""{"operation":"create_shift","payload":{"visibility":{{visibility}},"dayOfWeek":{{dayOfWeek}},"startTime":"08:00","endTime":"12:00","timeZone":"UTC","totalSeats":4},"commit":true}""");

        AgentToolResult result = await tool.ExecuteAsync(call, new AgentExecutionContext("run-1", new Dictionary<string, string>
        {
            ["confirmedAction"] = "true"
        }));

        result.IsError.Should().BeFalse(result.Output);
        written.Should().NotBeNull();
        written!.Visibility.Should().Be(Vitarish.Appointments.Domain.Enums.ShiftVisibility.Public);
    }

    [Fact]
    public async Task Appointment_administration_refuses_an_unreadable_payload_before_proposing_it()
    {
        Mock<IAccessControlledShiftManager> shifts = new(MockBehavior.Strict);
        AppointmentAdministrationAgentTool tool = new(
            new Mock<IAccessControlledReservationManager>().Object,
            shifts.Object,
            new Mock<IServiceManager>().Object,
            new Mock<IServiceTypeManager>().Object,
            new Mock<IServiceProviderManager>().Object,
            new Mock<IShiftBookingPolicyManager>().Object,
            new Mock<IShiftReminderPolicyManager>().Object,
            new Mock<IShiftBookableDateQueryService>().Object,
            new Mock<ITemporaryHoldManager>().Object);
        AgentToolCall call = new("call-1", "appointment_manage",
            """{"operation":"create_shift","payload":{"visibility":"Everyone","dayOfWeek":"Monday","startTime":"08:00","endTime":"12:00","timeZone":"UTC","totalSeats":4}}""");

        AgentToolResult result = await tool.ExecuteAsync(call, new AgentExecutionContext("run-1"));

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("invalid");
        tool.GetPreparedAction(result).Should().BeNull();
    }
    [Fact]
    public async Task Patient_search_returns_candidates_in_the_shape_the_result_parser_reads()
    {
        Mock<IAssistantPatientSearch> search = new(MockBehavior.Strict);
        search.Setup(service => service.SearchAsync("practice-1", "owner-1", "run-1", "Sara", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssistantSearchResult([new AssistantSearchCandidate("opaque", "Sara", null, "1******9", "0******1")], false));
        SearchPatientsTool tool = new(search.Object, new Mock<IAppointmentSchedulingAgentCapability>(MockBehavior.Strict).Object);

        AgentToolResult result = await tool.ExecuteAsync(new AgentToolCall("call-1", "search_patients", """{"search":"Sara"}"""), PracticeExecution());

        AssistantSearchResult? parsed = System.Text.Json.JsonSerializer.Deserialize<AssistantSearchResult>(result.Output);
        parsed!.Candidates.Should().ContainSingle(candidate => candidate.CandidateToken == "opaque");
        parsed.HasMore.Should().BeFalse();
    }

    [Theory]
    [InlineData("2026-11-14T00:00:00")]
    [InlineData("14/11/2026")]
    public async Task An_unreadable_date_is_returned_to_the_model_as_a_correctable_tool_error(string dateFrom)
    {
        Mock<IAppointmentSchedulingAgentCapability> scheduling = new(MockBehavior.Strict);
        ListOperationsAppointmentsTool handler = new(scheduling.Object);
        AssistantContext context = new(
            "practice-1", Guid.Empty, "owner-1", new HashSet<string>(StringComparer.Ordinal),
            AssistantOperationsScope.TenantOwnerRole, "en-US", "UTC", "conversation-1", SubjectKind: "practice");
        AgentExecutionContext execution = PracticeExecution();
        IAgentTool guarded = new AssistantToolDispatcher(new AssistantCapabilityRegistry(
                [], new AssistantAuthorizationPolicy(), new AssistantToolAvailabilityPolicy(), []))
            .CreateGuardedTools(context, execution, [handler], new Dictionary<string, string> { [handler.Definition.Name] = "Operations" })
            .Single();

        AgentToolResult result = await guarded.ExecuteAsync(
            new AgentToolCall("call-1", "list_operations_appointments", $$"""{"dateFrom":"{{dateFrom}}"}"""), execution);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("dateFrom must be a Gregorian date written as yyyy-MM-dd");
        scheduling.VerifyNoOtherCalls();
    }

    private static AgentExecutionContext PracticeExecution() => new("run-1", new Dictionary<string, string>
    {
        ["practiceId"] = "practice-1",
        ["patientId"] = Guid.Empty.ToString("D"),
        ["authenticatedUserId"] = "owner-1",
        ["subjectKind"] = "practice"
    });
}
