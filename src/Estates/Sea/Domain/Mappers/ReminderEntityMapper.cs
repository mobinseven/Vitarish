#nullable enable

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Reminder entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    /// <summary>
    /// Converts a Reminder entity to its read DTO representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Dto.ReminderReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Reminder src);

    /// <summary>
    /// Converts a Reminder write DTO to its entity representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Entities.Reminder ToDomain(global::Vitarish.Sea.Domain.Dto.ReminderWriteDto src);

    /// <summary>
    /// Converts a Reminder descriptor DTO to its descriptor representation.
    /// </summary>
    public partial global::Vitarish.Sea.Domain.Models.ReminderDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ReminderDescriptorDto src);
}
