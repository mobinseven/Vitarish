namespace Vitarish.Entities.Contracts.Models;

public sealed record PartedResult<T>(IReadOnlyList<T> Items, int TotalItems);