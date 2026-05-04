using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for OccurrenceProjection entity (read-only view; no write DTO).
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.OccurrenceProjectionReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.OccurrenceProjection src);

    public partial global::Vitarish.Calendar.Domain.Models.OccurrenceProjectionDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.OccurrenceProjectionDescriptorDto src);
}
