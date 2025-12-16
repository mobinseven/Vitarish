namespace Vitarish.Entities.Server.Models;

public sealed record PartedResult<T>(IReadOnlyList<T> Items, int TotalItems);