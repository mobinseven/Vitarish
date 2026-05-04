using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for CalendarParticipant entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.CalendarParticipantReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.CalendarParticipant src);

    public partial global::Vitarish.Calendar.Domain.Entities.CalendarParticipant ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarParticipantWriteDto src);

    public partial global::Vitarish.Calendar.Domain.Dto.CalendarParticipantWriteDto ToWrite(global::Vitarish.Calendar.Domain.Dto.CalendarParticipantReadDto src);

    public partial global::Vitarish.Calendar.Domain.Models.CalendarParticipantDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarParticipantDescriptorDto src);
}
