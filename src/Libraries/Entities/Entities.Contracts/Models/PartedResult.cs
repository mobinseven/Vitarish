namespace Vitarish.Entities.Contracts;

public sealed record PartedResult<T>(IReadOnlyList<T> Items, int TotalItems);