/// <summary>
/// Mapping extension methods for Reminders domain entities.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Reminders.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Reminders.Domain.Dto.ReminderReadDto ToDto(this global::Vitarish.Reminders.Domain.Entities.Reminder entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Reminders.Domain.Entities.Reminder ToDomain(this global::Vitarish.Reminders.Domain.Dto.ReminderWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Reminders.Domain.Dto.ReminderWriteDto ToWrite(this global::Vitarish.Reminders.Domain.Dto.ReminderReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Reminders.Domain.Models.ReminderDescriptor ToDomain(this global::Vitarish.Reminders.Domain.Dto.ReminderDescriptorDto dto) => Mapper.ToDomain(dto);
}
