using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.AgentCapabilities.ClinicalEvents;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Infrastructure.AgentCapabilities.ClinicalEvents;

public sealed class SeaClinicalEventProcedureBoundary(IProceduresService procedures, IEventsService events) : IClinicalEventProcedureBoundary
{
    public async Task<PatientProcedureContext?> GetContextAsync(ClinicalEventScope scope, Guid procedureId, CancellationToken cancellationToken)
    {
        EntityResult<ProcedureReadDto> result = await procedures.GetAsync(procedureId, cancellationToken);
        ProcedureReadDto? procedure = result.GetValueOrDefault();
        if (procedure is null || procedure.ProjectId != scope.PatientId) return null;
        IReadOnlyList<EventReadDto> existing = await events.GetByProcedureAsync(procedureId, cancellationToken);
        string versionInput = string.Join('|', existing.Select(item => $"{item.Id:D}:{item.UpdatedAt:O}:{item.IsCommitted}"));
        string version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(versionInput)))[..16];
        return new(procedure.Id, procedure.ProjectId, procedure.ProjectId, procedure.Name,
            new HashSet<int> { existing.Count + 1 }, version, CanRecordClinicalEvent: true);
    }

    public async Task<Guid?> AddClinicalEventItemAsync(ClinicalEventScope scope, ClinicalEventContent content, ClinicalEventProvenance provenance, CancellationToken cancellationToken)
    {
        PatientProcedureContext? current = await GetContextAsync(scope, content.ProcedureId, cancellationToken);
        if (current is null || !current.ValidVisitOrSessionNumbers.Contains(content.VisitOrSessionNumber)) return null;
        IReadOnlyList<EventReadDto> existing = await events.GetByProcedureAsync(content.ProcedureId, cancellationToken);
        Guid? previousEventId = existing.LastOrDefault()?.Id;
        string structuredEnvelope = JsonSerializer.Serialize(new { content, provenance });
        EventReadDto? created = await events.AddAsync(current.ProjectId, content.ProcedureId, previousEventId,
            $"{content.EventType} — visit/session {content.VisitOrSessionNumber}", structuredEnvelope, cancellationToken);
        return created?.Id;
    }
}
