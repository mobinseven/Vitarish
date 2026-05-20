/// <summary>
/// Mapping extension methods for Appointments domain entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Appointments.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Appointments.Domain.Dto.ShiftReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Shift entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Shift ToDomain(this global::Vitarish.Appointments.Domain.Dto.ShiftWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ShiftWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ShiftReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.ShiftDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.ShiftDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ReservationReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Reservation entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Reservation ToDomain(this global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ReservationReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.ReservationDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.ReservationDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.ShiftReminderPolicy entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.ShiftReminderPolicy ToDomain(this global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ShiftReminderPolicyReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.ShiftBookingPolicy ToDomain(this global::Vitarish.Appointments.Domain.Dto.ShiftBookingPolicyWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ShiftReservationReadDto ToDto(this global::Vitarish.Appointments.Domain.Models.ShiftReservation model) => Mapper.ToDto(model);

    public static global::Vitarish.Appointments.Domain.Dto.ServiceTypeReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.ServiceType entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.ServiceType ToDomain(this global::Vitarish.Appointments.Domain.Dto.ServiceTypeWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ServiceTypeWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ServiceTypeReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.ServiceTypeDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.ServiceTypeDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ServiceReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Service entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Service ToDomain(this global::Vitarish.Appointments.Domain.Dto.ServiceWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ServiceWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ServiceReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.ServiceDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.ServiceDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ServiceProviderReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.ServiceProvider entity)
    {
        global::Vitarish.Appointments.Domain.Dto.ServiceProviderReadDto dto = Mapper.ToDtoBase(entity);
        return dto with { LinkedUserIds = [.. entity.UserLinks.Select(l => l.UserId)] };
    }

    public static global::Vitarish.Appointments.Domain.Entities.ServiceProvider ToDomain(this global::Vitarish.Appointments.Domain.Dto.ServiceProviderWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ServiceProviderWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ServiceProviderReadDto dto)
    {
        global::Vitarish.Appointments.Domain.Dto.ServiceProviderWriteDto writeDto = Mapper.ToWrite(dto);
        return writeDto with { LinkedUserIds = [.. dto.LinkedUserIds] };
    }

    public static global::Vitarish.Appointments.Domain.Models.ServiceProviderDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.ServiceProviderDescriptorDto dto) => Mapper.ToDomain(dto);
}
