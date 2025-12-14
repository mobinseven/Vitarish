namespace Vitarish.Clients.Modules.Sea.Dto;

using System;
using System.Collections.Generic;

public sealed record ProcedureSnapshot(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ProjectDescription,
    string ProjectMetadata,
    string Name,
    string Metadata,
    string? CurrentOverview,
    DateTimeOffset? OverviewUpdatedAt,
    StatsDto? Stats,
    IReadOnlyList<EventSnapshot> Events);
