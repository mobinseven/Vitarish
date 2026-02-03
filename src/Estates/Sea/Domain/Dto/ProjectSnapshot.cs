namespace Vitarish.Sea.Domain.Dto;

using System;
using System.Collections.Generic;

public sealed record ProjectSnapshot(
    Guid Id,
    string Name,
    string Description,
    bool Template,
    string Metadata,
    string? CurrentOverview,
    DateTimeOffset? OverviewUpdatedAt,
    StatsDto? Stats,
    IReadOnlyList<ProcedureSnapshot> Procedures);
