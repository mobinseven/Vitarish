using System.Text.Json.Serialization;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;

/// <summary>What the procedure read tools return to the model, in the clinic's words (ADR-0023 decision 5).</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProcedureListResponse))]
[JsonSerializable(typeof(StepListResponse))]
[JsonSerializable(typeof(StepDetailResponse))]
[JsonSerializable(typeof(FileListResponse))]
internal sealed partial class ProcedureReadJsonSerializerContext : JsonSerializerContext;

public sealed record ProcedureListResponse(IReadOnlyList<ProcedureSummary> Procedures, bool HasMore);

/// <summary>A procedure with where it stands: the last committed step and the next step still open.</summary>
public sealed record ProcedureSummary(
    Guid Id,
    string Name,
    string? Description,
    StatsReadDto? Stats,
    DateTime SourceUpdatedAt,
    int StepCount,
    int CommittedStepCount,
    StepReference? CurrentStep,
    StepReference? NextStep);

/// <summary>A step named by its 1-based position; id and title are withheld when access rules restrict the step.</summary>
public sealed record StepReference(Guid? Id, string? Title, int Position, DateTime? CommittedAt, bool Restricted);

public sealed record StepListResponse(bool Found, string? ProcedureName = null, IReadOnlyList<StepSummary>? Steps = null, bool HasMore = false);

public sealed record StepSummary(
    Guid? Id,
    string? Title,
    int Position,
    bool IsCommitted,
    DateTime? CommittedAt,
    DateTime? SourceUpdatedAt,
    int? ItemCount,
    bool Restricted);

public sealed record StepDetailResponse(bool Found, bool Restricted = false, StepDetail? Step = null);

public sealed record StepDetail(
    Guid Id,
    string Title,
    string? Description,
    int Position,
    bool IsCommitted,
    DateTime? CommittedAt,
    DateTime SourceUpdatedAt,
    Guid ProcedureId,
    string? ProcedureName,
    IReadOnlyList<StepNote> Notes,
    bool MoreNotes,
    IReadOnlyList<StepFile> Files,
    bool MoreFiles,
    int ReminderCount);

public sealed record StepNote(Guid Id, string Text, bool Truncated, DateTime CreatedAt);

public sealed record StepFile(Guid Id, string? FileName, string? ContentType, long Size, DateTime CreatedAt);

public sealed record FileListResponse(bool Found, bool Restricted = false, IReadOnlyList<StepFile>? Files = null, bool HasMore = false);
