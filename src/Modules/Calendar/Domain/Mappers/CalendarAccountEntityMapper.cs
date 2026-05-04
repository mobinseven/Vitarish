using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for CalendarAccount entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.CalendarAccountReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.CalendarAccount src);

    public partial global::Vitarish.Calendar.Domain.Entities.CalendarAccount ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarAccountWriteDto src);

    public partial global::Vitarish.Calendar.Domain.Dto.CalendarAccountWriteDto ToWrite(global::Vitarish.Calendar.Domain.Dto.CalendarAccountReadDto src);

    public partial global::Vitarish.Calendar.Domain.Models.CalendarAccountDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarAccountDescriptorDto src);
}
