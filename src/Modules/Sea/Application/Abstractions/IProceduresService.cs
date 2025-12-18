namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Sea.Domain.Dto;

public interface IProceduresService : IEntityService<ProcedureDto, ProcedureWriteDto, ProcedureDescriptorDto>
{
    Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken);
    Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken);
}
