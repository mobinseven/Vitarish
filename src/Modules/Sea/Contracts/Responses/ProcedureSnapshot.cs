namespace Vitarish.Sea.Contracts.Responses;

public sealed record ProcedureSnapshot(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ProjectDescription,
    string ProjectMetadata,
    string Name,
    string Metadata,
    string? CurrentOverview,
    DateTime? OverviewUpdatedAt,
    StatsReadDto? Stats,
    IReadOnlyList<EventSnapshot> Events);
