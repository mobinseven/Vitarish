#nullable enable

using Vitarish;

namespace Vitarish.Parties.Domain.Mappers;

/// <summary>
/// Auto-generated mapping extension methods for VitarishPartiesDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly EntityMapper Mapper = new EntityMapper();

    /// <summary>
    /// Converts a Owner entity to its read DTO representation.
    /// </summary>
    public static Dtos.OwnerReadDto ToDto(this Entities.Owner entity) => Mapper.ToDto(entity);

    /// <summary>
    /// Converts a Owner write DTO to its entity representation.
    /// </summary>
    public static Entities.Owner ToDomain(this Dtos.OwnerWriteDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a Owner descriptor DTO to its descriptor representation.
    /// </summary>
    public static Descriptors.OwnerDescriptor ToDomain(this Dtos.OwnerDescriptorDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a Tenant entity to its read DTO representation.
    /// </summary>
    public static Dtos.TenantReadDto ToDto(this Entities.Tenant entity) => Mapper.ToDto(entity);

    /// <summary>
    /// Converts a Tenant write DTO to its entity representation.
    /// </summary>
    public static Entities.Tenant ToDomain(this Dtos.TenantWriteDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a Tenant descriptor DTO to its descriptor representation.
    /// </summary>
    public static Descriptors.TenantDescriptor ToDomain(this Dtos.TenantDescriptorDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a TenantIndicator entity to its read DTO representation.
    /// </summary>
    public static Dtos.TenantIndicatorReadDto ToDto(this Entities.TenantIndicator entity) => Mapper.ToDto(entity);

    /// <summary>
    /// Converts a TenantIndicator write DTO to its entity representation.
    /// </summary>
    public static Entities.TenantIndicator ToDomain(this Dtos.TenantIndicatorWriteDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a TenantIndicator descriptor DTO to its descriptor representation.
    /// </summary>
    public static Descriptors.TenantIndicatorDescriptor ToDomain(this Dtos.TenantIndicatorDescriptorDto dto) => Mapper.ToDomain(dto);

}
