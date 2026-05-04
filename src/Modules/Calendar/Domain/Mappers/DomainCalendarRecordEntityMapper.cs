using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for DomainCalendarRecord entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.DomainCalendarRecord src);

    public partial global::Vitarish.Calendar.Domain.Entities.DomainCalendarRecord ToDomain(global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordWriteDto src);

    public partial global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordWriteDto ToWrite(global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordReadDto src);

    public partial global::Vitarish.Calendar.Domain.Models.DomainCalendarRecordDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.DomainCalendarRecordDescriptorDto src);
}
