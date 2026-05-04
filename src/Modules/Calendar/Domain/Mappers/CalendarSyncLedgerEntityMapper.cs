using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for CalendarSyncLedger entity (read-only; written internally by sync service).
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.CalendarSyncLedgerReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.CalendarSyncLedger src);

    public partial global::Vitarish.Calendar.Domain.Models.CalendarSyncLedgerDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.CalendarSyncLedgerDescriptorDto src);
}
