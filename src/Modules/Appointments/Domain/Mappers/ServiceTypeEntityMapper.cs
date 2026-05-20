using Riok.Mapperly.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for ServiceType entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ServiceTypeReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.ServiceType src);

    public partial global::Vitarish.Appointments.Domain.Entities.ServiceType ToDomain(global::Vitarish.Appointments.Domain.Dto.ServiceTypeWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ServiceTypeWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ServiceTypeReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ServiceTypeDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.ServiceTypeDescriptorDto src);
}
