using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Communications.Infrastructure.Serializers;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EntityDescriptorDto))]
[JsonSerializable(typeof(ReminderReadDto))]
[JsonSerializable(typeof(ReminderWriteDto))]
[JsonSerializable(typeof(ReminderDescriptorDto))]
[JsonSerializable(typeof(ReminderRecipientReadDto))]
[JsonSerializable(typeof(ReminderKind))]
[JsonSerializable(typeof(IReadOnlyList<ReminderReadDto>))]
[JsonSerializable(typeof(IReadOnlyList<ReminderWriteDto>))]
[JsonSerializable(typeof(IReadOnlyList<ReminderRecipientReadDto>))]
public partial class CommunicationsJsonSerializerContext : JsonSerializerContext;
