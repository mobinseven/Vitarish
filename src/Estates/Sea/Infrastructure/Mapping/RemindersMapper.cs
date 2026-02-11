namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class RemindersMapper
{
    public partial ReminderReadDto ToDto(Reminder src);
    public partial Reminder ToDomain(ReminderWriteDto src);
    public partial ReminderDescriptor ToDomain(ReminderDescriptorDto src);
}

