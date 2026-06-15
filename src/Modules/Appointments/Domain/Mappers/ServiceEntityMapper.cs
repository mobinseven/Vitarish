using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for Service entity.
/// </summary>
public partial class EntityMapper
{
    [MapProperty(nameof(Service.ServiceType) + "." + nameof(Service.ServiceType.Name), nameof(ServiceReadDto.ServiceTypeName))]
    public partial global::Vitarish.Appointments.Domain.Dto.ServiceReadDto ToRead(global::Vitarish.Appointments.Domain.Entities.Service src);

    public partial global::Vitarish.Appointments.Domain.Entities.Service ToEntity(global::Vitarish.Appointments.Domain.Dto.ServiceWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ServiceWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ServiceReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ServiceDescriptor ToDescriptor(global::Vitarish.Appointments.Domain.Dto.ServiceDescriptorDto src);
}
