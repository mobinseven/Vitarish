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
}
