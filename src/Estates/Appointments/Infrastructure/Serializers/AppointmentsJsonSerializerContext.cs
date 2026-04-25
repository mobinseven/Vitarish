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
public partial class AppointmentsJsonSerializerContext : JsonSerializerContext;
