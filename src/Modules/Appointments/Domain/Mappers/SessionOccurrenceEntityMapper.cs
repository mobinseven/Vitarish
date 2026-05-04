using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Entities;
using Vitarish.Appointments.Domain.Models;

namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for SessionOccurrence entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.SessionOccurrenceReadDto ToDto(global::Vitarish.Appointments.Domain.Entities.SessionOccurrence src);

    public partial global::Vitarish.Appointments.Domain.Models.SessionOccurrenceDescriptor ToDomain(global::Vitarish.Appointments.Domain.Dto.SessionOccurrenceDescriptorDto src);
}
