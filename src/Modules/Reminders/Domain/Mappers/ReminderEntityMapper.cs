using Vitarish.Reminders.Domain.Dto;
using Vitarish.Reminders.Domain.Entities;
using Vitarish.Reminders.Domain.Models;

namespace Vitarish.Reminders.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Reminders.Domain.Dto.ReminderReadDto ToDto(global::Vitarish.Reminders.Domain.Entities.Reminder src);

    public partial global::Vitarish.Reminders.Domain.Entities.Reminder ToDomain(global::Vitarish.Reminders.Domain.Dto.ReminderWriteDto src);

    public partial global::Vitarish.Reminders.Domain.Dto.ReminderWriteDto ToWrite(global::Vitarish.Reminders.Domain.Dto.ReminderReadDto src);

    public partial global::Vitarish.Reminders.Domain.Models.ReminderDescriptor ToDomain(global::Vitarish.Reminders.Domain.Dto.ReminderDescriptorDto src);
}
