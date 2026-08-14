using System.Security.Cryptography;
using System.Text;
using Vitarish.Clinic.Application.Events;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Events;

public sealed class SeaClinicalEventProcedureBoundary(
    IProjectsService projects,
    IProceduresService procedures,
    IEventsService events) : IClinicalEventProcedureBoundary
{
    public async Task<PatientProcedureContext?> GetContextAsync(
        Guid patientId,
        string practiceId,
        Guid procedureId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(practiceId);
        ProcedureReadDto? procedure = (await procedures.GetAsync(procedureId, cancellationToken)).Value;
        if (procedure is null || procedure.ProjectId != patientId) return null;
        ProjectReadDto? project = (await projects.GetAsync(patientId, cancellationToken)).Value;
        if (project is null) return null;

        IReadOnlyList<EventReadDto> existing = await events.GetByProcedureAsync(procedureId, cancellationToken);
        EventReadDto? previous = existing.LastOrDefault();
        return new PatientProcedureContext(
            procedure.ProjectId, procedure.Id, procedure.Name, existing.Count + 1, previous?.Id,
            Fingerprint(procedure, existing));
    }

    public async Task<Guid?> AddEventAsync(
        PatientProcedureContext context,
        string title,
        string opaquePayload,
        CancellationToken cancellationToken) =>
        (await events.AddAsync(
            context.ProjectId,
            context.ProcedureId,
            context.PreviousEventId,
            title,
            opaquePayload,
            cancellationToken))?.Id;

    private static string Fingerprint(
        ProcedureReadDto procedure,
        IReadOnlyList<EventReadDto> events)
    {
        string state = string.Join('|',
            new[] { procedure.ProjectId.ToString("D"), procedure.Id.ToString("D") }
                .Concat(events.Select(item =>
                    $"{item.Id:D}:{item.PreviousEventId:D}:{item.CommittedAt:O}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
    }
}
