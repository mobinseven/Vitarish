using Vitarish.Entities.Client.Models;
using Vitarish.Entities.Contracts;

namespace Vitarish.Entities.Client.SignalR;
/// <summary>
/// Client surface for working with entities over SignalR.
/// Interception is ambient (via an <c>IEntityInterceptionProvider</c>) and is
/// automatically applied to write operations. Read operations skip interception.
/// </summary>
public interface IEntityClient<TReadDto, TWriteDto, TDescriptorDto> : IAsyncDisposable
    where TReadDto : EntityReadDto
    where TWriteDto : EntityWriteDto
    where TDescriptorDto : EntityDescriptorDto
{
    /// <summary>
    /// Raised when the server pushes an entity notification.
    /// </summary>
    event EntityNotificationHandler? EntityNotificationReceived;

    /// <summary>
    /// Starts the underlying connection if not already started.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops the underlying connection if started.
    /// </summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Explicitly set the interception configuration for subsequent write calls.
    /// Most code won't need this if an ambient provider is used.
    /// </summary>
    Task SetInterceptionConfig(EntityInterceptionConfig? interceptionConfig);

    // ---------------------------------------------------------------------
    // Writes (interception applied automatically)
    // ---------------------------------------------------------------------
    Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto, CancellationToken ct = default);
    Task<EntityResult<IReadOnlyList<TReadDto>>> AddRangeAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken ct = default);
    Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto, CancellationToken ct = default);
    Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto, CancellationToken ct = default);
    Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateRangeAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken ct = default);

    Task<EntityResult> RemoveAsync(TWriteDto dto, CancellationToken ct = default);
    Task<EntityResult> RemoveRangeAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken ct = default);
    Task<EntityResult> RemoveByIdAsync(Guid id, CancellationToken ct = default);
    Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor, CancellationToken ct = default);

    // ---------------------------------------------------------------------
    // Reads (no interception)
    // ---------------------------------------------------------------------
    Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor, CancellationToken ct = default);
    Task<EntityResult<TReadDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor, CancellationToken ct = default);
    Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor, CancellationToken ct = default);
}
