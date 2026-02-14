#nullable enable

using Vitarish;


#nullable enable

using Vitarish.Sea.Domain;

namespace Vitarish.Sea.Domain.Mappers
{
    /// <summary>
    /// Auto-generated mapping extension methods for VitarishSeaDomain entities.
    /// These methods provide convenient mapping between Entity types and their DTOs.
    /// </summary>
    public static partial class EntityMappingExtensions
    {
        private static EntityMapper Mapper = new EntityMapper();
        /// <summary>
        /// Converts a Attachment entity to its read DTO representation.
        /// </summary>
        public static Dto.AttachmentReadDto ToDto(this Attachment entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Attachment write DTO to its entity representation.
        /// </summary>
        public static Attachment ToDomain(this Dto.AttachmentWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Attachment descriptor DTO to its descriptor representation.
        /// </summary>
        public static AttachmentDescriptor ToDomain(this Dto.AttachmentDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Event entity to its read DTO representation.
        /// </summary>
        public static Dto.EventReadDto ToDto(this Event entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Event write DTO to its entity representation.
        /// </summary>
        public static Event ToDomain(this Dto.EventWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Event descriptor DTO to its descriptor representation.
        /// </summary>
        public static EventDescriptor ToDomain(this Dto.EventDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a EventRole entity to its read DTO representation.
        /// </summary>
        public static Dto.EventRoleReadDto ToDto(this EventRole entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a EventRole write DTO to its entity representation.
        /// </summary>
        public static EventRole ToDomain(this Dto.EventRoleWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a EventRole descriptor DTO to its descriptor representation.
        /// </summary>
        public static EventRoleDescriptor ToDomain(this Dto.EventRoleDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Procedure entity to its read DTO representation.
        /// </summary>
        public static Dto.ProcedureReadDto ToDto(this Procedure entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Procedure write DTO to its entity representation.
        /// </summary>
        public static Procedure ToDomain(this Dto.ProcedureWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Procedure descriptor DTO to its descriptor representation.
        /// </summary>
        public static ProcedureDescriptor ToDomain(this Dto.ProcedureDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Project entity to its read DTO representation.
        /// </summary>
        public static Dto.ProjectReadDto ToDto(this Project entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Project write DTO to its entity representation.
        /// </summary>
        public static Project ToDomain(this Dto.ProjectWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Project descriptor DTO to its descriptor representation.
        /// </summary>
        public static ProjectDescriptor ToDomain(this Dto.ProjectDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Receipt entity to its read DTO representation.
        /// </summary>
        public static Dto.ReceiptReadDto ToDto(this Receipt entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Receipt write DTO to its entity representation.
        /// </summary>
        public static Receipt ToDomain(this Dto.ReceiptWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Receipt descriptor DTO to its descriptor representation.
        /// </summary>
        public static ReceiptDescriptor ToDomain(this Dto.ReceiptDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Reminder entity to its read DTO representation.
        /// </summary>
        public static Dto.ReminderReadDto ToDto(this Reminder entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Reminder write DTO to its entity representation.
        /// </summary>
        public static Reminder ToDomain(this Dto.ReminderWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Reminder descriptor DTO to its descriptor representation.
        /// </summary>
        public static ReminderDescriptor ToDomain(this Dto.ReminderDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Role entity to its read DTO representation.
        /// </summary>
        public static Dto.RoleReadDto ToDto(this Role entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a Role write DTO to its entity representation.
        /// </summary>
        public static Role ToDomain(this Dto.RoleWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a Role descriptor DTO to its descriptor representation.
        /// </summary>
        public static RoleDescriptor ToDomain(this Dto.RoleDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a UserRole entity to its read DTO representation.
        /// </summary>
        public static Dto.UserRoleReadDto ToDto(this UserRole entity)
        {
            return Mapper.ToDto(entity);
        }

        /// <summary>
        /// Converts a UserRole write DTO to its entity representation.
        /// </summary>
        public static UserRole ToDomain(this Dto.UserRoleWriteDto dto)
        {
            return Mapper.ToDomain(dto);
        }

        /// <summary>
        /// Converts a UserRole descriptor DTO to its descriptor representation.
        /// </summary>
        public static UserRoleDescriptor ToDomain(this Dto.UserRoleDescriptorDto dto)
        {
            return Mapper.ToDomain(dto);
        }

    }
}
