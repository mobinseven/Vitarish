using MediatR;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Commands.Visits;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.Clinic.Application.Handlers;

public sealed class StartVisitPrescriptionDraftHandler(
    IPatientDirectory patients,
    IClinicActorContext actorContext,
    IVisitsEventWriter visits,
    IPrescriptionDraftWriter prescriptions,
    IOperationReceiptStore<VisitPrescriptionOperation> operations)
    : IRequestHandler<StartVisitPrescriptionDraftCommand, StartVisitPrescriptionDraftResult>
{
    private const int MaxProviderKeyLength = 64;
    private const int MaxTitleLength = 256;
    private const int MaxIdempotencyKeyLength = 128;
    private static readonly TimeSpan ActiveReceiptLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan ExceptionalReceiptLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan EventCreationLease = TimeSpan.FromMinutes(2);

    public async Task<StartVisitPrescriptionDraftResult> Handle(
        StartVisitPrescriptionDraftCommand request,
        CancellationToken cancellationToken)
    {
        string providerKey = request.ProviderKey.Trim();
        string eventTitle = request.EventTitle.Trim();
        string idempotencyKey = request.IdempotencyKey.Trim();
        if (request.PatientId == Guid.Empty ||
            providerKey.Length is 0 or > MaxProviderKeyLength ||
            eventTitle.Length is 0 or > MaxTitleLength ||
            idempotencyKey.Length is 0 or > MaxIdempotencyKeyLength)
        {
            return new(StartVisitPrescriptionDraftOutcome.ValidationFailure);
        }

        if (!actorContext.TryGetActor(out ClinicActor actor))
        {
            return new(StartVisitPrescriptionDraftOutcome.DependencyFailure, Message: "Clinic actor context is unavailable.");
        }

        OperationReceiptKey receiptKey = new(
            "clinic.visit-prescription",
            actor.TenantId,
            idempotencyKey);
        OperationReceipt<VisitPrescriptionOperation>? receipt;
        bool ownsEventLease = false;
        try
        {
            receipt = await operations.GetAsync(receiptKey, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(StartVisitPrescriptionDraftOutcome.DependencyFailure, Message: "Operation receipt storage is unavailable.");
        }

        if (receipt is null)
        {
            PatientView? patient = await patients.FindByIdAsync(request.PatientId, cancellationToken);
            if (patient is null)
            {
                return new(StartVisitPrescriptionDraftOutcome.PatientNotFound);
            }

            VisitPrescriptionOperation claimed = new(
                Guid.NewGuid(),
                actor.TenantId,
                idempotencyKey,
                request.PatientId,
                providerKey,
                eventTitle,
                Guid.NewGuid(),
                VisitPrescriptionOperationState.CreatingEvent,
                LeaseExpiresAt: DateTime.UtcNow.Add(EventCreationLease));
            try
            {
                if (await operations.TryClaimAsync(
                    receiptKey,
                    claimed,
                    ActiveReceiptLifetime,
                    cancellationToken))
                {
                    receipt = new(0, claimed);
                    ownsEventLease = true;
                }
                else
                {
                    receipt = await operations.GetAsync(receiptKey, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new(StartVisitPrescriptionDraftOutcome.DependencyFailure, Message: "Operation receipt storage is unavailable.");
            }

            if (receipt is null)
            {
                return new(StartVisitPrescriptionDraftOutcome.DependencyFailure);
            }
        }

        VisitPrescriptionOperation operation = receipt.Value;
        if (operation.PatientId != request.PatientId ||
            !string.Equals(operation.ProviderKey, providerKey, StringComparison.Ordinal) ||
            !string.Equals(operation.EventTitle, eventTitle, StringComparison.Ordinal))
        {
            return new(StartVisitPrescriptionDraftOutcome.ValidationFailure, Message: "Idempotency key is already used for another request.");
        }

        if (operation.State == VisitPrescriptionOperationState.Completed)
        {
            return StartVisitPrescriptionDraftResult.Success(operation.EventId!.Value, operation.PrescriptionId);
        }

        if (operation.State == VisitPrescriptionOperationState.UnknownMutationOutcome)
        {
            return new(
                StartVisitPrescriptionDraftOutcome.UnknownMutationOutcome,
                operation.EventId,
                operation.PrescriptionId,
                operation.Failure);
        }

        if (operation.State == VisitPrescriptionOperationState.CreatingEvent &&
            operation.LeaseExpiresAt > DateTime.UtcNow &&
            !ownsEventLease)
        {
            return new(
                StartVisitPrescriptionDraftOutcome.InProgress,
                operation.EventId,
                operation.PrescriptionId);
        }

        if (operation.State == VisitPrescriptionOperationState.CreatingEvent &&
            !ownsEventLease)
        {
            operation = operation with
            {
                State = VisitPrescriptionOperationState.UnknownMutationOutcome,
                Failure = "Visit creation did not reach a known terminal outcome.",
                LeaseExpiresAt = null
            };
            _ = await TrySaveAsync(
                operations,
                receiptKey,
                receipt.Revision,
                operation,
                ExceptionalReceiptLifetime,
                CancellationToken.None);
            return new(
                StartVisitPrescriptionDraftOutcome.UnknownMutationOutcome,
                operation.EventId,
                operation.PrescriptionId,
                operation.Failure);
        }

        PatientView? currentPatient = await patients.FindByIdAsync(request.PatientId, cancellationToken);
        if (currentPatient is null)
        {
            return new(StartVisitPrescriptionDraftOutcome.PatientNotFound);
        }

        if (operation.EventId is null)
        {
            if (!ownsEventLease)
            {
                operation = operation with
                {
                    State = VisitPrescriptionOperationState.CreatingEvent,
                    LeaseExpiresAt = DateTime.UtcNow.Add(EventCreationLease)
                };
                if (!await TrySaveAsync(
                    operations,
                    receiptKey,
                    receipt.Revision,
                    operation,
                    ActiveReceiptLifetime,
                    cancellationToken))
                {
                    return new(StartVisitPrescriptionDraftOutcome.InProgress, PrescriptionId: operation.PrescriptionId);
                }

                receipt = new(receipt.Revision + 1, operation);
            }
            VisitEventCreationResult eventResult;
            try
            {
                eventResult = await visits.CreateAsync(
                    operation.PatientId,
                    operation.EventTitle,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                operation = operation with
                {
                    State = VisitPrescriptionOperationState.UnknownMutationOutcome,
                    Failure = "Visit creation returned an unknown mutation outcome.",
                    LeaseExpiresAt = null
                };
                _ = await TrySaveAsync(
                    operations,
                    receiptKey,
                    receipt.Revision,
                    operation,
                    ExceptionalReceiptLifetime,
                    CancellationToken.None);
                return new(
                    StartVisitPrescriptionDraftOutcome.UnknownMutationOutcome,
                    PrescriptionId: operation.PrescriptionId);
            }

            if (!eventResult.Succeeded || eventResult.EventId is null || eventResult.ProcedureId is null)
            {
                operation = operation with
                {
                    State = VisitPrescriptionOperationState.ReadyToRetry,
                    Failure = eventResult.Error,
                    LeaseExpiresAt = null
                };
                _ = await TrySaveAsync(
                    operations,
                    receiptKey,
                    receipt.Revision,
                    operation,
                    ActiveReceiptLifetime,
                    cancellationToken);
                return new(StartVisitPrescriptionDraftOutcome.DependencyFailure, Message: eventResult.Error);
            }

            operation = operation with
            {
                State = VisitPrescriptionOperationState.CreatingPrescription,
                ProcedureId = eventResult.ProcedureId,
                EventId = eventResult.EventId,
                LeaseExpiresAt = null
            };
            if (!await TrySaveAsync(
                operations,
                receiptKey,
                receipt.Revision,
                operation,
                ActiveReceiptLifetime,
                cancellationToken))
            {
                return new(
                    StartVisitPrescriptionDraftOutcome.RecoverablePartialFailure,
                    operation.EventId,
                    operation.PrescriptionId);
            }

            receipt = new(receipt.Revision + 1, operation);
        }

        PrescriptionDraftCreationResult prescriptionResult;
        try
        {
            prescriptionResult = await prescriptions.CreateAsync(
                operation.PrescriptionId,
                request.PatientId,
                currentPatient.IdentificationCode,
                actor.DoctorId,
                actor.ClinicId,
                operation.EventId!.Value,
                operation.ProviderKey,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(
                StartVisitPrescriptionDraftOutcome.RecoverablePartialFailure,
                operation.EventId,
                operation.PrescriptionId,
                "Prescription creation is temporarily unavailable.");
        }

        if (prescriptionResult.Succeeded)
        {
            operation = operation with
            {
                State = VisitPrescriptionOperationState.Completed,
                Failure = null
            };
            if (!await TrySaveAsync(
                operations,
                receiptKey,
                receipt.Revision,
                operation,
                ActiveReceiptLifetime,
                cancellationToken))
            {
                return new(
                    StartVisitPrescriptionDraftOutcome.RecoverablePartialFailure,
                    operation.EventId,
                    operation.PrescriptionId);
            }

            return StartVisitPrescriptionDraftResult.Success(operation.EventId.Value, operation.PrescriptionId);
        }

        bool compensated;
        try
        {
            compensated = await visits.RemoveEmptyAsync(
                operation.ProcedureId!.Value,
                operation.EventId.Value,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            compensated = false;
        }

        operation = operation with
        {
            State = compensated
                ? VisitPrescriptionOperationState.ReadyToRetry
                : VisitPrescriptionOperationState.RecoverablePartialFailure,
            ProcedureId = compensated ? null : operation.ProcedureId,
            EventId = compensated ? null : operation.EventId,
            Failure = prescriptionResult.Error
        };
        _ = await TrySaveAsync(
            operations,
            receiptKey,
            receipt.Revision,
            operation,
            compensated ? ActiveReceiptLifetime : ExceptionalReceiptLifetime,
            cancellationToken);

        return new(
            compensated
                ? StartVisitPrescriptionDraftOutcome.DependencyFailure
                : StartVisitPrescriptionDraftOutcome.RecoverablePartialFailure,
            operation.EventId,
            operation.PrescriptionId,
            prescriptionResult.Error);
    }

    private static async Task<bool> TrySaveAsync(
        IOperationReceiptStore<VisitPrescriptionOperation> operations,
        OperationReceiptKey key,
        long revision,
        VisitPrescriptionOperation operation,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operations.TryUpdateAsync(
                key,
                revision,
                operation,
                lifetime,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }
}
