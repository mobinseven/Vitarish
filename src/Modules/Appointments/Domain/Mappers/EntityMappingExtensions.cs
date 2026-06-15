namespace Vitarish.Appointments.Domain.Mappers;


/// <summary>
/// Mapping extension methods for Appointments domain entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static Dto.ShiftReadDto ToRead(this Shift entity) => Mapper.ToRead(entity);

    public static Shift ToEntity(this Dto.ShiftWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ShiftWriteDto ToWrite(this Dto.ShiftReadDto dto) => Mapper.ToWrite(dto);

    public static Models.ShiftDescriptor ToDescriptor(this Dto.ShiftDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ReservationReadDto ToRead(this Reservation entity) => Mapper.ToRead(entity);

    public static Reservation ToEntity(this Dto.ReservationWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ReservationWriteDto ToWrite(this Dto.ReservationReadDto dto) => Mapper.ToWrite(dto);

    public static Models.ReservationDescriptor ToDescriptor(this Dto.ReservationDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ShiftReminderPolicyReadDto ToRead(this ShiftReminderPolicy entity) => Mapper.ToRead(entity);

    public static ShiftReminderPolicy ToEntity(this Dto.ShiftReminderPolicyWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ShiftReminderPolicyWriteDto ToWrite(this Dto.ShiftReminderPolicyReadDto dto) => Mapper.ToWrite(dto);

    public static Dto.ShiftBookingPolicyReadDto ToRead(this ShiftBookingPolicy entity) => Mapper.ToRead(entity);

    public static ShiftBookingPolicy ToEntity(this Dto.ShiftBookingPolicyWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ShiftBookingPolicyWriteDto ToWrite(this Dto.ShiftBookingPolicyReadDto dto) => Mapper.ToWrite(dto);

    public static Models.ShiftBookingPolicyDescriptor ToDescriptor(this Dto.ShiftBookingPolicyDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Models.ShiftReminderPolicyDescriptor ToDescriptor(this Dto.ShiftReminderPolicyDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ShiftReservationReadDto ToRead(this Models.ShiftReservation model) => Mapper.ToRead(model);

    public static Dto.ServiceTypeReadDto ToRead(this ServiceType entity) => Mapper.ToRead(entity);

    public static ServiceType ToEntity(this Dto.ServiceTypeWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ServiceTypeWriteDto ToWrite(this Dto.ServiceTypeReadDto dto) => Mapper.ToWrite(dto);

    public static Models.ServiceTypeDescriptor ToDescriptor(this Dto.ServiceTypeDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ServiceReadDto ToRead(this Service entity) => Mapper.ToRead(entity);

    public static Service ToEntity(this Dto.ServiceWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ServiceWriteDto ToWrite(this Dto.ServiceReadDto dto) => Mapper.ToWrite(dto);

    public static Models.ServiceDescriptor ToDescriptor(this Dto.ServiceDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ServiceProviderReadDto ToRead(this ServiceProvider entity)
    {
        Dto.ServiceProviderReadDto dto = Mapper.ToRead(entity);
        return dto with { LinkedUserIds = [.. entity.UserLinks.Select(l => l.UserId)] };
    }

    public static ServiceProvider ToEntity(this Dto.ServiceProviderWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ServiceProviderWriteDto ToWrite(this Dto.ServiceProviderReadDto dto)
    {
        Dto.ServiceProviderWriteDto writeDto = Mapper.ToWrite(dto);
        return writeDto with { LinkedUserIds = [.. dto.LinkedUserIds] };
    }

    public static Models.ServiceProviderDescriptor ToDescriptor(this Dto.ServiceProviderDescriptorDto dto) => Mapper.ToDescriptor(dto);
}
