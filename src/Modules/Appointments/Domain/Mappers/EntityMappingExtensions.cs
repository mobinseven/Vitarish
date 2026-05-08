/// <summary>
/// Mapping extension methods for Appointments domain entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Appointments.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Appointments.Domain.Dto.SessionReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Session entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Session ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.SessionWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.SessionReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.SessionDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ReservationReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Reservation entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Reservation ToDomain(this global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.ReservationWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.ReservationReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.ReservationDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.ReservationDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.SessionOccurrenceReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.SessionOccurrence entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Models.SessionOccurrenceDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionOccurrenceDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.SessionReminderPolicy entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.SessionReminderPolicy ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.SessionReminderPolicyDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionReminderPolicyDescriptorDto dto) => Mapper.ToDomain(dto);
}
