using Vitarish.Calendar.Domain.Dto;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Domain.Mappers;

/// <summary>
/// Mapper declarations for AvailabilityPolicy entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyReadDto ToDto(global::Vitarish.Calendar.Domain.Entities.AvailabilityPolicy src);

    public partial global::Vitarish.Calendar.Domain.Entities.AvailabilityPolicy ToDomain(global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyWriteDto src);

    public partial global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyWriteDto ToWrite(global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyReadDto src);

    public partial global::Vitarish.Calendar.Domain.Models.AvailabilityPolicyDescriptor ToDomain(global::Vitarish.Calendar.Domain.Dto.AvailabilityPolicyDescriptorDto src);
}
