using MediatR;

namespace Vitarish.Clinic.Application.Commands.Visits;

public sealed record StartVisitPrescriptionDraftCommand(
    Guid PatientId,
    string ProviderKey,
    string EventTitle,
    string IdempotencyKey) : IRequest<StartVisitPrescriptionDraftResult>;

public sealed record StartVisitPrescriptionDraftResult(
    StartVisitPrescriptionDraftOutcome Outcome,
    Guid? EventId = null,
    Guid? PrescriptionId = null,
    string? Message = null)
{
    public static StartVisitPrescriptionDraftResult Success(Guid eventId, Guid prescriptionId) =>
        new(StartVisitPrescriptionDraftOutcome.Succeeded, eventId, prescriptionId);
}

public enum StartVisitPrescriptionDraftOutcome
{
    Succeeded,
    ValidationFailure,
    PatientNotFound,
    InProgress,
    DependencyFailure,
    RecoverablePartialFailure,
    UnknownMutationOutcome
}
