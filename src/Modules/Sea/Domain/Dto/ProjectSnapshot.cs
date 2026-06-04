namespace Vitarish.Sea.Domain.Dto;

public sealed record ProjectSnapshot(
    Guid Id,
    string Name,
    string Description,
    bool Template,
    string Metadata,
    string? CurrentOverview,
    DateTime? OverviewUpdatedAt,
    StatsReadDto? Stats,
    IReadOnlyList<ProcedureSnapshot> Procedures);
