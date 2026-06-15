namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for Shift entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ShiftReadDto ToRead(global::Vitarish.Appointments.Domain.Entities.Shift src);

    public partial global::Vitarish.Appointments.Domain.Entities.Shift ToEntity(global::Vitarish.Appointments.Domain.Dto.ShiftWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ShiftWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ShiftReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ShiftDescriptor ToDescriptor(global::Vitarish.Appointments.Domain.Dto.ShiftDescriptorDto src);
}
