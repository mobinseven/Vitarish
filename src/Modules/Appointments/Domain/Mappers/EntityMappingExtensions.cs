/// <summary>
/// Mapping extension methods for Appointments domain entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Appointments.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Appointments.Domain.Dto.AppointmentReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Appointment entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Appointment ToDomain(this global::Vitarish.Appointments.Domain.Dto.AppointmentWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.AppointmentWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.AppointmentReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.AppointmentDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.AppointmentDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.SessionReadDto ToDto(this global::Vitarish.Appointments.Domain.Entities.Session entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Appointments.Domain.Entities.Session ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Appointments.Domain.Dto.SessionWriteDto ToWrite(this global::Vitarish.Appointments.Domain.Dto.SessionReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Appointments.Domain.Models.SessionDescriptor ToDomain(this global::Vitarish.Appointments.Domain.Dto.SessionDescriptorDto dto) => Mapper.ToDomain(dto);
}
