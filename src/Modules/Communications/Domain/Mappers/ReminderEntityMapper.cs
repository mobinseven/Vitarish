namespace Vitarish.Communications.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Communications.Domain.Dto.ReminderReadDto ToRead(global::Vitarish.Communications.Domain.Entities.Reminder src);

    public partial global::Vitarish.Communications.Domain.Entities.Reminder ToEntity(global::Vitarish.Communications.Domain.Dto.ReminderWriteDto src);

    public partial global::Vitarish.Communications.Domain.Dto.ReminderWriteDto ToWrite(global::Vitarish.Communications.Domain.Dto.ReminderReadDto src);

    public partial global::Vitarish.Communications.Domain.Models.ReminderDescriptor ToDescriptor(global::Vitarish.Communications.Domain.Dto.ReminderDescriptorDto src);
}
