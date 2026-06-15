namespace Vitarish.Appointments.Domain.Mappers;

/// <summary>
/// Mapper declarations for Reservation entity.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Appointments.Domain.Dto.ReservationReadDto ToRead(global::Vitarish.Appointments.Domain.Entities.Reservation src);

    public partial global::Vitarish.Appointments.Domain.Entities.Reservation ToEntity(global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto src);

    public partial global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto ToWrite(global::Vitarish.Appointments.Domain.Dto.ReservationReadDto src);

    public partial global::Vitarish.Appointments.Domain.Models.ReservationDescriptor ToDescriptor(global::Vitarish.Appointments.Domain.Dto.ReservationDescriptorDto src);
}
