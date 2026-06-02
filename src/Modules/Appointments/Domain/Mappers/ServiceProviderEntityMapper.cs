using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ServiceProvider entity.
/// </summary>
public partial class EntityMapper
{
    [MapperIgnoreSource(nameof(ServiceProvider.UserLinks))]
    public partial global::Vitarish.Appointments.Domain.Dto.ServiceProviderReadDto ToDtoBase(global::Vitarish.Appointments.Domain.Entities.ServiceProvider src);

    [MapperIgnoreSource(nameof(ServiceProvider.UserLinks))]
    public partial global::Vitarish.Appointments.Domain.Entities.ServiceProvider ToDomain(global::Vitarish.Appointments.Domain.Dto.ServiceProviderWriteDto src);

    [MapperIgnoreTarget(nameof(ServiceProviderWriteDto.LinkedUserIds))]
    public partial global::Vitarish.Appointments.Domain.Dto.ServiceProviderWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ServiceProviderReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ServiceProviderDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.ServiceProviderDescriptorDto src);
}
