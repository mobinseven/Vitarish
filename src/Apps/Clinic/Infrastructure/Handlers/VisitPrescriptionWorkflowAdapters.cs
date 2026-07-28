using MediatR;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class RequestClinicActorContext(IRequestContext requestContext) : IClinicActorContext
{
    public bool TryGetActor(out ClinicActor actor)
    {
        if (!string.IsNullOrWhiteSpace(requestContext.TenantId) &&
            Guid.TryParse(requestContext.TenantId, out Guid clinicId) &&
            Guid.TryParse(requestContext.OwnerId, out Guid doctorId))
        {
            actor = new(requestContext.TenantId.Trim(), clinicId, doctorId);
            return true;
        }

        actor = null!;
        return false;
    }
}

internal sealed class SeaVisitsEventWriter(
    IProceduresService procedures,
    IEventsService events) : IVisitsEventWriter
{
    public async Task<VisitEventCreationResult> CreateAsync(
        Guid patientId,
        string title,
        CancellationToken cancellationToken)
    {
        var listResult = await procedures.GetListAsync(
            new ProcedureDescriptorDto { ProjectId = patientId },
            cancellationToken);
        if (!listResult.IsSuccess)
        {
            return new(false, Error: "Visits procedure lookup failed.");
        }

        ProcedureReadDto? procedure = listResult.Value?
            .SingleOrDefault(item => string.Equals(
                item.Name,
                CreatePatientVisitsProcedureHandler.ReservedProcedureName,
                StringComparison.Ordinal));
        if (procedure is null)
        {
            var createResult = await procedures.AddAsync(new ProcedureWriteDto
            {
                ProjectId = patientId,
                Name = CreatePatientVisitsProcedureHandler.ReservedProcedureName,
                Description = string.Empty
            }, cancellationToken);
            procedure = createResult.IsSuccess ? createResult.Value : null;
        }

        if (procedure is null)
        {
            return new(false, Error: "Visits procedure is unavailable.");
        }

        EventReadDto? visit = await events.AddAsync(
            patientId,
            procedure.Id,
            previousEventId: null,
            title,
            string.Empty,
            cancellationToken);
        return visit is null
            ? new(false, procedure.Id, Error: "Visit event creation failed.")
            : new(true, procedure.Id, visit.Id);
    }

    public async Task<bool> RemoveEmptyAsync(
        Guid procedureId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        EventSnapshot? snapshot = await events.CaptureSnapshotAsync(
            eventId,
            cancellationToken);
        if (snapshot is null || snapshot.Committed || snapshot.Attachments.Count != 0)
        {
            return false;
        }

        return await events.RemoveEventAsync(
            procedureId,
            eventId,
            cancellationToken);
    }
}

internal sealed class MedicalPrescriptionDraftWriter(ISender sender) : IPrescriptionDraftWriter
{
    public async Task<PrescriptionDraftCreationResult> CreateAsync(
        Guid prescriptionId,
        Guid patientId,
        string? patientNationalId,
        Guid doctorId,
        Guid clinicId,
        Guid encounterId,
        string providerKey,
        CancellationToken cancellationToken)
    {
        CreateDraftResult result = await sender.Send(new CreateDraftCommand
        {
            PrescriptionId = prescriptionId,
            PatientId = patientId,
            PatientNationalId = patientNationalId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            EncounterId = encounterId,
            SelectedProviderKey = providerKey
        }, cancellationToken);
        return new(result.IsSuccess, result.PrescriptionId, result.ErrorMessage);
    }
}
