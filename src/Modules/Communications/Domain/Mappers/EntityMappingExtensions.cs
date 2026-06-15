/// <summary>
/// Mapping extension methods for Communications reminder entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Communications.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Communications.Domain.Dto.ReminderReadDto ToRead(this global::Vitarish.Communications.Domain.Entities.Reminder entity) => Mapper.ToRead(entity);

    public static global::Vitarish.Communications.Domain.Entities.Reminder ToEntity(this global::Vitarish.Communications.Domain.Dto.ReminderWriteDto dto) => Mapper.ToEntity(dto);

    public static global::Vitarish.Communications.Domain.Dto.ReminderWriteDto ToWrite(this global::Vitarish.Communications.Domain.Dto.ReminderReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Communications.Domain.Models.ReminderDescriptor ToDescriptor(this global::Vitarish.Communications.Domain.Dto.ReminderDescriptorDto dto) => Mapper.ToDescriptor(dto);
}
