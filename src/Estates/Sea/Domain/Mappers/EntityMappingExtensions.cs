
/// <summary>
/// Auto-generated mapping extension methods for VitarishSeaDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly global::Vitarish.Sea.Domain.Mappers.EntityMapper Mapper = new();

    public static global::Vitarish.Sea.Domain.Dto.AttachmentReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Attachment entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Attachment ToDomain(this global::Vitarish.Sea.Domain.Dto.AttachmentWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.AttachmentWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.AttachmentReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.AttachmentDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.AttachmentDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.EventReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Event entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Event ToDomain(this global::Vitarish.Sea.Domain.Dto.EventWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.EventWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.EventReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.EventDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.EventDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.EventRoleReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.EventRole entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.EventRole ToDomain(this global::Vitarish.Sea.Domain.Dto.EventRoleWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.EventRoleWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.EventRoleReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.EventRoleDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.EventRoleDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ProcedureReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Procedure entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Procedure ToDomain(this global::Vitarish.Sea.Domain.Dto.ProcedureWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ProcedureWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.ProcedureReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.ProcedureDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.ProcedureDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ProjectReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Project entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Project ToDomain(this global::Vitarish.Sea.Domain.Dto.ProjectWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ProjectWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.ProjectReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.ProjectDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.ProjectDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ReceiptReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Receipt entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Receipt ToDomain(this global::Vitarish.Sea.Domain.Dto.ReceiptWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ReceiptWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.ReceiptReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.ReceiptDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.ReceiptDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ReminderReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Reminder entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Reminder ToDomain(this global::Vitarish.Sea.Domain.Dto.ReminderWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.ReminderWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.ReminderReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.ReminderDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.ReminderDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.RoleReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.Role entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.Role ToDomain(this global::Vitarish.Sea.Domain.Dto.RoleWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.RoleWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.RoleReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.RoleDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.RoleDescriptorDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.UserRoleReadDto ToDto(this global::Vitarish.Sea.Domain.Entities.UserRole entity) => Mapper.ToDto(entity);

    public static global::Vitarish.Sea.Domain.Entities.UserRole ToDomain(this global::Vitarish.Sea.Domain.Dto.UserRoleWriteDto dto) => Mapper.ToDomain(dto);

    public static global::Vitarish.Sea.Domain.Dto.UserRoleWriteDto ToWrite(this global::Vitarish.Sea.Domain.Dto.UserRoleReadDto dto) => Mapper.ToWrite(dto);

    public static global::Vitarish.Sea.Domain.Models.UserRoleDescriptor ToDomain(this global::Vitarish.Sea.Domain.Dto.UserRoleDescriptorDto dto) => Mapper.ToDomain(dto);

}
