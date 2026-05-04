using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for CalendarAuditEntry entity (append-only; only ToDto exposed).
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.CalendarAuditEntryReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.CalendarAuditEntry src);
}
