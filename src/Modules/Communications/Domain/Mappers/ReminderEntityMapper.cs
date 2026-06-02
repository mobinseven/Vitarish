namespace Vitarish.Communications.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Communications.Domain.Dto.ReminderReadDto ToDto(global::Vitarish.Communications.Domain.Entities.Reminder src);

    public partial global::Vitarish.Communications.Domain.Entities.Reminder ToDomain(global::Vitarish.Communications.Domain.Dto.ReminderWriteDto src);

    public partial global::Vitarish.Communications.Domain.Dto.ReminderWriteDto ToWrite(global::Vitarish.Communications.Domain.Dto.ReminderReadDto src);

    public partial global::Vitarish.Communications.Domain.Models.ReminderDescriptor ToDomain(global::Vitarish.Communications.Domain.Dto.ReminderDescriptorDto src);
}
