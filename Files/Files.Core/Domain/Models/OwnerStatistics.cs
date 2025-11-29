namespace Vitarish.Files.Core.Domain.Models;

/// <summary>
/// Statistics for file storage by owner/tenant.
/// </summary>
/// <param name="Owner">The owner/tenant ID.</param>
/// <param name="Count">Number of files owned.</param>
/// <param name="TotalSize">Total size in bytes of all files.</param>
public record OwnerStatistics(string Owner, int Count, long TotalSize);
