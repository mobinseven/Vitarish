using System.Text.Json;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;

/// <summary>Which assistant a procedure tool serves, and so how it learns the patient.</summary>
public enum ProcedureToolWorkspace
{
    /// <summary>The patient page: the patient is the server-bound subject of the run.</summary>
    Patient,
    /// <summary>The practice workspace: the patient is a candidate token from this run's patient search.</summary>
    Operations
}

/// <summary>
/// The one reader of a patient's procedures, steps, notes and files for the Clinic assistants. Every step is checked
/// against the workflow access rules for the signed-in user, so a step the user cannot open on screen is reported
/// as restricted, never read.
/// </summary>
public sealed class PatientProcedureRecords(
    IProceduresService procedures,
    IEventsService events,
    IAttachmentsService attachments,
    ISeaManagementAccessEvaluator access,
    IAssistantPatientSearch patientSearch)
{
    public const int MaxNoteLength = 2000;
    public const int MaxNotes = 10;
    public const int MaxFiles = 20;
    public const string CandidateTokenArgument = "candidateToken";

    /// <summary>The patient this call is about: the run's patient, or the patient of a candidate token from this run.</summary>
    public async Task<Guid> ResolvePatientAsync(ProcedureToolWorkspace workspace, JsonElement arguments, AgentExecutionContext context, CancellationToken ct)
    {
        if (workspace == ProcedureToolWorkspace.Patient)
        {
            return Metadata(context, "patientId") is { } raw && Guid.TryParse(raw, out Guid patientId) && patientId != Guid.Empty
                ? patientId
                : throw new InvalidOperationException("The trusted patient scope is incomplete.");
        }

        string token = arguments.TryGetProperty(CandidateTokenArgument, out JsonElement value) && value.GetString() is { Length: > 0 } text
            ? text
            : throw new JsonException("candidateToken is required; search for the patient first.");
        AssistantCandidateReference? candidate = await patientSearch.ResolveAsync(
            Required(context, "practiceId"), Required(context, "authenticatedUserId"), context.ExecutionId, token, ct);
        return candidate?.PatientId ?? throw new JsonException("The patient candidate is invalid or expired; search for the patient again in this conversation turn.");
    }

    public async Task<ProcedureListResponse> ListProceduresAsync(Guid patientId, AgentExecutionContext context, int limit, CancellationToken ct)
    {
        SeaManagementAgentSubject subject = Subject(context);
        IReadOnlyList<ProcedureReadDto> values = (await procedures.GetListAsync(new ProcedureDescriptorDto { ProjectId = patientId }, ct)).Value ?? [];
        List<ProcedureSummary> result = [];
        foreach (ProcedureReadDto procedure in values.Take(limit))
        {
            IReadOnlyList<EventReadDto> steps = await events.GetByProcedureAsync(procedure.Id, ct);
            await access.PrimeEventRolesAsync(steps.Select(step => step.Id), ct);
            int current = -1;
            for (int index = 0; index < steps.Count; index++)
                if (steps[index].IsCommitted) current = index;
            int next = -1;
            for (int index = 0; index < steps.Count; index++)
            {
                if (!steps[index].IsCommitted) { next = index; break; }
            }

            result.Add(new(
                procedure.Id, procedure.Name, procedure.Description, procedure.Stats, procedure.UpdatedAt ?? procedure.CreatedAt,
                steps.Count, steps.Count(step => step.IsCommitted),
                current < 0 ? null : await ReferenceAsync(subject, steps[current], current, ct),
                next < 0 ? null : await ReferenceAsync(subject, steps[next], next, ct)));
        }

        return new(result, values.Count > limit);
    }

    public async Task<StepListResponse> ListStepsAsync(Guid patientId, Guid procedureId, AgentExecutionContext context, int limit, CancellationToken ct)
    {
        ProcedureReadDto? procedure = await ProcedureAsync(patientId, procedureId, ct);
        if (procedure is null) return new(false);
        SeaManagementAgentSubject subject = Subject(context);
        IReadOnlyList<EventReadDto> steps = await events.GetByProcedureAsync(procedureId, ct);
        await access.PrimeEventRolesAsync(steps.Select(step => step.Id), ct);
        List<StepSummary> result = [];
        for (int index = 0; index < steps.Count && result.Count < limit; index++)
        {
            EventReadDto step = steps[index];
            result.Add(await access.CanReadEventAsync(subject, step.Id, ct)
                ? new(step.Id, step.Title, index + 1, step.IsCommitted, step.CommittedAt, step.UpdatedAt ?? step.CreatedAt, step.Stats?.EventItems, false)
                : new(null, null, index + 1, step.IsCommitted, null, null, null, true));
        }

        return new(true, procedure.Name, result, steps.Count > limit);
    }

    public async Task<StepDetailResponse> GetStepAsync(Guid patientId, Guid stepId, AgentExecutionContext context, CancellationToken ct)
    {
        (EventReadDto? step, bool restricted) = await StepAsync(patientId, stepId, context, ct);
        if (step is null) return new(false, restricted);
        ProcedureReadDto? procedure = (await procedures.GetAsync(step.ProcedureId, ct)).Value;
        IReadOnlyList<EventReadDto> chain = await events.GetByProcedureAsync(step.ProcedureId, ct);
        int position = chain.Select((item, index) => (item.Id, index)).FirstOrDefault(item => item.Id == step.Id).index + 1;
        IReadOnlyList<AttachmentReadDto> items = await attachments.GetByEventAsync(step.Id, ct);
        AttachmentReadDto[] notes = [.. items.Where(item => item.Kind == AttachmentKind.Text).OrderByDescending(item => item.CreatedAt)];
        AttachmentReadDto[] files = [.. items.Where(item => item.Kind == AttachmentKind.File).OrderByDescending(item => item.CreatedAt)];
        int reminders = await events.GetRemindersCountAsync(step.Id, ct);
        return new(true, false, new StepDetail(
            step.Id, step.Title, step.Description, position, step.IsCommitted, step.CommittedAt, step.UpdatedAt ?? step.CreatedAt,
            step.ProcedureId, procedure?.Name,
            [.. notes.Take(MaxNotes).Select(note => new StepNote(note.Id, Truncate(note.Content), note.Content.Length > MaxNoteLength, note.CreatedAt))],
            notes.Length > MaxNotes,
            [.. files.Take(MaxFiles).Select(File)],
            files.Length > MaxFiles,
            reminders));
    }

    public async Task<FileListResponse> ListFilesAsync(Guid patientId, Guid stepId, AgentExecutionContext context, int limit, CancellationToken ct)
    {
        (EventReadDto? step, bool restricted) = await StepAsync(patientId, stepId, context, ct);
        if (step is null) return new(false, restricted);
        AttachmentReadDto[] files = [.. (await attachments.GetByEventAsync(step.Id, ct)).Where(item => item.Kind == AttachmentKind.File).OrderByDescending(item => item.CreatedAt)];
        return new(true, false, [.. files.Take(limit).Select(File)], files.Length > limit);
    }

    /// <summary>A step of this patient that the user may read; null with <c>restricted</c> when access rules hide it.</summary>
    public async Task<(EventReadDto? Step, bool Restricted)> StepAsync(Guid patientId, Guid stepId, AgentExecutionContext context, CancellationToken ct)
    {
        EventReadDto? step = (await events.GetAsync(stepId, ct)).Value;
        if (step is null || step.ProjectId != patientId) return (null, false);
        return await access.CanReadEventAsync(Subject(context), step.Id, ct) ? (step, false) : (null, true);
    }

    /// <summary>A procedure of this patient, or null.</summary>
    public async Task<ProcedureReadDto?> ProcedureAsync(Guid patientId, Guid procedureId, CancellationToken ct)
    {
        ProcedureReadDto? procedure = (await procedures.GetAsync(procedureId, ct)).Value;
        return procedure?.ProjectId == patientId ? procedure : null;
    }

    public static SeaManagementAgentSubject Subject(AgentExecutionContext context) =>
        new(Required(context, "practiceId"), Required(context, "authenticatedUserId"));

    private async Task<StepReference> ReferenceAsync(SeaManagementAgentSubject subject, EventReadDto step, int index, CancellationToken ct) =>
        await access.CanReadEventAsync(subject, step.Id, ct)
            ? new(step.Id, step.Title, index + 1, step.CommittedAt, false)
            : new(null, null, index + 1, null, true);

    private static StepFile File(AttachmentReadDto item) => new(item.Id, item.FileName, item.ContentType, item.Size, item.CreatedAt);

    private static string Truncate(string text) => text.Length <= MaxNoteLength ? text : text[..MaxNoteLength];

    private static string? Metadata(AgentExecutionContext context, string key) =>
        context.Metadata is not null && context.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string Required(AgentExecutionContext context, string key) =>
        Metadata(context, key) ?? throw new InvalidOperationException($"The trusted assistant scope is missing {key}.");
}
