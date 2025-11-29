namespace Vitarish.Files.Core.Services;

using Vitarish.Files.Core.Domain.Models;

/// <summary>
/// Service interface for managing file information.
/// </summary>
public interface IFileInformationService
{
    /// <summary>
    /// Gets a file information by ID.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file information DTO if found; otherwise, null.</returns>
    Task<FileInformationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all file information for an owner.
    /// </summary>
    /// <param name="owner">The owner/tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of file information DTOs.</returns>
    Task<IEnumerable<FileInformationDto>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new file information entry.
    /// </summary>
    /// <param name="fileInfo">The file information to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created file information DTO.</returns>
    Task<FileInformationDto> CreateAsync(FileInformationDto fileInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing file information entry.
    /// </summary>
    /// <param name="fileInfo">The file information to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated file information DTO.</returns>
    Task<FileInformationDto> UpdateAsync(FileInformationDto fileInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file information entry.
    /// </summary>
    /// <param name="id">The file ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage statistics for an owner.
    /// </summary>
    /// <param name="owner">The owner/tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The owner statistics.</returns>
    Task<OwnerStatistics?> GetOwnerStatisticsAsync(string owner, CancellationToken cancellationToken = default);
}
