namespace Vitarish.Appointments.Infrastructure.Serializers;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EntityDescriptorDto))]
[JsonSerializable(typeof(AppointmentReadDto))]
[JsonSerializable(typeof(AppointmentWriteDto))]
[JsonSerializable(typeof(AppointmentDescriptorDto))]
[JsonSerializable(typeof(AppointmentStatus))]
[JsonSerializable(typeof(AppointmentSortBy))]
[JsonSerializable(typeof(IReadOnlyList<AppointmentReadDto>))]
[JsonSerializable(typeof(IReadOnlyList<AppointmentWriteDto>))]
[JsonSerializable(typeof(SessionReadDto))]
[JsonSerializable(typeof(SessionWriteDto))]
[JsonSerializable(typeof(SessionDescriptorDto))]
[JsonSerializable(typeof(SessionStatus))]
[JsonSerializable(typeof(SessionSortBy))]
[JsonSerializable(typeof(IReadOnlyList<SessionReadDto>))]
[JsonSerializable(typeof(IReadOnlyList<SessionWriteDto>))]
public partial class AppointmentsJsonSerializerContext : JsonSerializerContext;
