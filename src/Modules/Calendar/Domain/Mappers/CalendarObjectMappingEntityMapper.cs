using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for CalendarObjectMapping entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.CalendarObjectMapping src);

    public partial global::Vitarish.Calendar.Domain.Entities.CalendarObjectMapping ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingWriteDto src);

    public partial global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingWriteDto ToWrite(global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingReadDto src);

    public partial global::Vitarish.Calendar.Domain.Models.CalendarObjectMappingDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarObjectMappingDescriptorDto src);
}
