/// <summary>
/// Mapping extension methods for Communications reminder entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Communications.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Communications.Domain.Dto.ReminderReadDto ToDto(this global::Vitarish.Communications.Domain.Entities.Reminder entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Communications.Domain.Entities.Reminder ToDomain(this global::Vitarish.Communications.Domain.Dto.ReminderWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Communications.Domain.Dto.ReminderWriteDto ToWrite(this global::Vitarish.Communications.Domain.Dto.ReminderReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Communications.Domain.Models.ReminderDescriptor ToDomain(this global::Vitarish.Communications.Domain.Dto.ReminderDescriptorDto dto) => Mapper.ToDomain(dto);
}
