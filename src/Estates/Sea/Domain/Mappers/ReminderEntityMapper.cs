
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Reminder entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.ReminderReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Reminder src);

    public partial global::Vitarish.Sea.Domain.Entities.Reminder ToDomain(global::Vitarish.Sea.Domain.Dto.ReminderWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.ReminderWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.ReminderReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.ReminderDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ReminderDescriptorDto src);
}
