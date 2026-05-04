using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for Session entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.SessionReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.Session src);

    public partial global::Vitarish.Appointments.Domain.Entities.Session ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.SessionWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.SessionReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.SessionDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionDescriptorDto src);
}
