using System.Text.Json;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public sealed class ProjectObjectRefreshUnavailableException(
    Guid objectId,
    Guid concurrencyToken,
    Exception innerException) : Exception(
        "The project-object materialized view could not be refreshed.",
        innerException)
{
    public Guid ObjectId { get; } = objectId;
    public Guid ConcurrencyToken { get; } = concurrencyToken;
}

public interface IProjectObjectService
{
    Task<ProjectObjectReadDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectObjectReadDto>> GetByOwnerAsync(
        string ownerEntityType,
        Guid ownerEntityId,
        string? objectType,
        CancellationToken cancellationToken);

    Task<ProjectObjectReadDto> CreateAsync(
        string ownerEntityType,
        Guid ownerEntityId,
        string objectType,
        JsonElement data,
        CancellationToken cancellationToken,
        Guid? id = null);

    Task<ProjectObjectReadDto?> UpdateAsync(
        Guid id,
        string ownerEntityType,
        Guid ownerEntityId,
        string objectType,
        JsonElement data,
        Guid concurrencyToken,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken);
}
