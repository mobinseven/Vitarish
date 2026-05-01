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
[JsonSerializable(typeof(AppointmentSessionReadDto))]
[JsonSerializable(typeof(AppointmentSessionWriteDto))]
[JsonSerializable(typeof(AppointmentSessionDescriptorDto))]
[JsonSerializable(typeof(AppointmentSessionStatus))]
[JsonSerializable(typeof(AppointmentSessionSortBy))]
[JsonSerializable(typeof(AppointmentSessionStats))]
[JsonSerializable(typeof(IReadOnlyList<AppointmentSessionReadDto>))]
[JsonSerializable(typeof(IReadOnlyList<AppointmentSessionWriteDto>))]
public partial class AppointmentsJsonSerializerContext : JsonSerializerContext;
