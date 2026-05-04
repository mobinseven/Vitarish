using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledSessionManager(
    IRequestContext requestContext,
    ISessionManager sessionManager) : IAccessControlledSessionManager
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    // ── Reads (any authenticated user) ────────────────────────────────────────

    public Task<EntityResult<SessionReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => sessionManager.GetAsync(id, cancellationToken);

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> GetListAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionManager.GetListAsync(descriptor, cancellationToken);

    public Task<EntityResult<PartedResult<SessionReadDto>>> GetPartedAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionManager.GetPartedAsync(descriptor, cancellationToken);

    public Task<EntityResult<int>> CountAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionManager.CountAsync(descriptor, cancellationToken);

    // ── Writes (admin only) ────────────────────────────────────────────────────

    public Task<EntityResult<SessionReadDto>> AddAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> AddAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<SessionReadDto>> UpdateAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> UpdateAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<SessionReadDto>> AddOrUpdateAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    // ── Lifecycle (admin only) ─────────────────────────────────────────────────

    public Task<EntityResult> ActivateSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.ActivateSessionAsync(sessionId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> PauseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.PauseSessionAsync(sessionId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.CloseSessionAsync(sessionId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> CancelSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.CancelSessionAsync(sessionId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> ArchiveSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => IsAdmin ? sessionManager.ArchiveSessionAsync(sessionId, cancellationToken) : throw AdminRoleRequired;
}
