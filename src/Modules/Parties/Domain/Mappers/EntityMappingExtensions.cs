using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Domain.Mappers;


/// <summary>
/// Auto-generated mapping extension methods for VitarishPartiesDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly EntityMapper Mapper = new();


    public static OwnerReadDto ToRead(this Owner entity) => Mapper.ToRead(entity);
    public static Owner ToEntity(this OwnerWriteDto dto) => Mapper.ToEntity(dto);
    public static OwnerWriteDto ToWrite(this OwnerReadDto dto) => Mapper.ToWrite(dto);
    public static OwnerDescriptor ToDescriptor(this OwnerDescriptorDto dto) => Mapper.ToDescriptor(dto);


    public static TenantReadDto ToRead(this Tenant entity) => Mapper.ToRead(entity);
    public static Tenant ToEntity(this TenantWriteDto dto) => Mapper.ToEntity(dto);
    public static TenantWriteDto ToWrite(this TenantReadDto dto) => Mapper.ToWrite(dto);
    public static TenantWriteDto ToWriteWithMetadata(this TenantReadDto dto)
    {
        TenantWriteDto write = dto.ToWrite();
        write.Metadata = new Dictionary<string, object?>(dto.Metadata);
        return write;
    }
    public static TenantDescriptor ToDescriptor(this TenantDescriptorDto dto) => Mapper.ToDescriptor(dto);


    public static TenantIndicatorReadDto ToRead(this TenantIndicator entity) => Mapper.ToRead(entity);
    public static TenantIndicator ToEntity(this TenantIndicatorWriteDto dto) => Mapper.ToEntity(dto);
    public static TenantIndicatorDescriptor ToDescriptor(this TenantIndicatorDescriptorDto dto) => Mapper.ToDescriptor(dto);

}
