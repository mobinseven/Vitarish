namespace Vitarish.Clinic.Application.Abstractions;

public interface IClinicActorContext
{
    bool TryGetActor(out ClinicActor actor);
}

public sealed record ClinicActor(string TenantId, Guid ClinicId, Guid DoctorId);

public interface IVisitsEventWriter
{
    Task<VisitEventCreationResult> CreateAsync(
        Guid patientId,
        string title,
        CancellationToken cancellationToken);

    Task<bool> RemoveEmptyAsync(
        Guid procedureId,
        Guid eventId,
        CancellationToken cancellationToken);
}

public sealed record VisitEventCreationResult(
    bool Succeeded,
    Guid? ProcedureId = null,
    Guid? EventId = null,
    string? Error = null);

public interface IPrescriptionDraftWriter
{
    Task<PrescriptionDraftCreationResult> CreateAsync(
        Guid prescriptionId,
        Guid patientId,
        string? patientNationalId,
        Guid doctorId,
        Guid clinicId,
        Guid encounterId,
        string providerKey,
        CancellationToken cancellationToken);
}

public sealed record PrescriptionDraftCreationResult(
    bool Succeeded,
    Guid? PrescriptionId = null,
    string? Error = null);

public sealed record VisitPrescriptionOperation(
    Guid Id,
    string TenantId,
    string IdempotencyKey,
    Guid PatientId,
    string ProviderKey,
    string EventTitle,
    Guid PrescriptionId,
    VisitPrescriptionOperationState State,
    Guid? ProcedureId = null,
    Guid? EventId = null,
    string? Failure = null,
    DateTime? LeaseExpiresAt = null);

public enum VisitPrescriptionOperationState
{
    CreatingEvent,
    ReadyToRetry,
    CreatingPrescription,
    Completed,
    RecoverablePartialFailure,
    UnknownMutationOutcome
}
