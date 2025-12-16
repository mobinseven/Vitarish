namespace Vitarish.Entities.Server.Services;

public interface IEntityService<TReadDto, TWriteDto, TDescriptorDto>
where TReadDto : EntityReadDto
where TWriteDto : EntityWriteDto
where TDescriptorDto : EntityDescriptorDto
{
    Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<TReadDto>>> AddAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken);
    Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken);
    Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult> RemoveAsync(TWriteDto dto, CancellationToken cancellationToken);
    Task<EntityResult> RemoveAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken);
    Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken);
    Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<TReadDto>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor, CancellationToken cancellationToken);
}