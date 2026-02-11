namespace Vitarish.Sea.Domain.Dto;

using System.Collections.Generic;

public sealed record GodViewPage(IReadOnlyList<GodViewRowReadDto> Items, int TotalCount);
