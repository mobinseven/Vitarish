namespace Vitarish.Sea.Infrastructure.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class RemindersMapper
{
    public partial ReminderDto ToDto(Reminder src);
    public partial Reminder ToDomain(ReminderDto src);
    public partial ReminderDescriptor ToDomain(ReminderDescriptorDto src);
}

