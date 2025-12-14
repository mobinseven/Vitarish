namespace Vitarish.Clients.Modules.Sea.Dto;

using System.Collections.Generic;

public sealed record GodViewPage(IReadOnlyList<GodViewRowDto> Items, int TotalCount);
