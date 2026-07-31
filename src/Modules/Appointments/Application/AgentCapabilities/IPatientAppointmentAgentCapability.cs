namespace Vitarish.Appointments.Application.AgentCapabilities;

public interface IPatientAppointmentAgentCapability
{
    Task<AppointmentSlot?> CheckAvailabilityAsync(PatientAppointmentRequest request, CancellationToken cancellationToken);
    Task<AppointmentProposalResult> PrepareAsync(PatientAppointmentRequest request, CancellationToken cancellationToken);
    Task<AppointmentCommitResult> ConfirmAsync(Guid proposalId, Guid patientId, string practiceId, CancellationToken cancellationToken);
}

public sealed class PatientAppointmentAgentCapability(
    CheckPatientAppointmentAvailability check,
    PreparePatientAppointment prepare,
    ConfirmPatientAppointmentProposal confirm) : IPatientAppointmentAgentCapability
{
    public Task<AppointmentSlot?> CheckAvailabilityAsync(PatientAppointmentRequest request, CancellationToken cancellationToken) => check.ExecuteAsync(request, cancellationToken);
    public Task<AppointmentProposalResult> PrepareAsync(PatientAppointmentRequest request, CancellationToken cancellationToken) => prepare.ExecuteAsync(request, cancellationToken);
    public Task<AppointmentCommitResult> ConfirmAsync(Guid proposalId, Guid patientId, string practiceId, CancellationToken cancellationToken) => confirm.ExecuteAsync(proposalId, patientId, practiceId, cancellationToken);
}
