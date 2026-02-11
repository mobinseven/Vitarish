namespace Vitarish.Entities.Server.Services;

/// <summary>
/// Interface for mapping between Entity types and their corresponding DTOs.
/// This interface is intended to be implemented by auto-generated mappers.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TReadDto">The read DTO type</typeparam>
/// <typeparam name="TWriteDto">The write DTO type</typeparam>
/// <typeparam name="TDescriptor">The descriptor type</typeparam>
/// <typeparam name="TDescriptorDto">The descriptor DTO type</typeparam>
public interface IEntityMapper<TEntity, TReadDto, TWriteDto, TDescriptor, TDescriptorDto>
    where TEntity : Core.Entity
    where TReadDto : Models.EntityReadDto
    where TWriteDto : Models.EntityWriteDto
    where TDescriptor : Core.EntityDescriptor
    where TDescriptorDto : Models.EntityDescriptorDto
{
    /// <summary>
    /// Maps an entity to a read DTO.
    /// </summary>
    TReadDto ToDto(TEntity entity);

    /// <summary>
    /// Maps a write DTO to an entity.
    /// </summary>
    TEntity ToDomain(TWriteDto dto);

    /// <summary>
    /// Maps a descriptor DTO to a descriptor.
    /// </summary>
    TDescriptor ToDomain(TDescriptorDto dto);
}
