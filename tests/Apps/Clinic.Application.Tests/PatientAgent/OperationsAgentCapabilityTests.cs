using FluentAssertions;
using Moq;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.PatientAgent;

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
        Mock<IPatientAgentPatientSearch> search = new(MockBehavior.Strict);
        search.Setup(service => service.ResolveAsync("practice-1", "owner-1", "run-1", "opaque-candidate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PatientAgentCandidateReference(patientId, "practice-1", "owner-1", "run-1", "Masked patient", "1******9", "0******1"));
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
}
