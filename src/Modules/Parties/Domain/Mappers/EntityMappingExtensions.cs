using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Entities;

/// <summary>
/// Auto-generated mapping extension methods for VitarishPartiesDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly Vitarish.Parties.Domain.Mappers.EntityMapper Mapper = new();

    /// <summary>
    /// Converts a Owner entity to its read DTO representation.
    /// </summary>
    public static OwnerReadDto ToDto(this Owner entity) => Mapper.ToDto(entity);

    /// <summary>
    /// Converts a Owner write DTO to its entity representation.
    /// </summary>
    public static Owner ToDomain(this OwnerWriteDto dto) => Mapper.ToDomain(dto);
    public static OwnerWriteDto ToWrite(this OwnerReadDto dto) => Mapper.ToWrite(dto);

    /// <summary>
    /// Converts a Owner descriptor DTO to its descriptor representation.
    /// </summary>
    public static OwnerDescriptor ToDomain(this OwnerDescriptorDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a Tenant entity to its read DTO representation.
    /// </summary>
    public static TenantReadDto ToDto(this Tenant entity) => Mapper.ToDto(entity);

    /// <summary>
    /// Converts a Tenant write DTO to its entity representation.
    /// </summary>
    public static Tenant ToDomain(this TenantWriteDto dto) => Mapper.ToDomain(dto);
    public static TenantWriteDto ToWrite(this TenantReadDto dto) => Mapper.ToWrite(dto);

    /// <summary>
    /// Converts a Tenant read DTO to a write DTO while preserving tenant metadata.
    /// </summary>
    public static TenantWriteDto ToWriteWithMetadata(this TenantReadDto dto)
    {
        TenantWriteDto write = dto.ToWrite();
        write.Metadata = new Dictionary<string, object?>(dto.Metadata);
        return write;
    }

    /// <summary>
    /// Converts a Tenant descriptor DTO to its descriptor representation.
    /// </summary>
    public static TenantDescriptor ToDomain(this TenantDescriptorDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a TenantIndicator entity to its read DTO representation.
    /// </summary>
    public static TenantIndicatorReadDto ToDto(this TenantIndicator entity) => Mapper.ToDto(entity);

    /// <summary>
    /// Converts a TenantIndicator write DTO to its entity representation.
    /// </summary>
    public static TenantIndicator ToDomain(this TenantIndicatorWriteDto dto) => Mapper.ToDomain(dto);

    /// <summary>
    /// Converts a TenantIndicator descriptor DTO to its descriptor representation.
    /// </summary>
    public static TenantIndicatorDescriptor ToDomain(this TenantIndicatorDescriptorDto dto) => Mapper.ToDomain(dto);

}
