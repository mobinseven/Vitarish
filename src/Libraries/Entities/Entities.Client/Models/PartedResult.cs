namespace Vitarish.Entities.Client.Models;

public sealed record PartedResult<T>(IReadOnlyList<T> Items, int TotalItems);