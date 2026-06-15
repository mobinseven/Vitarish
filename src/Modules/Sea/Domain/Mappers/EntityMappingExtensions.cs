namespace Vitarish.Sea.Domain.Mappers;


/// <summary>
/// Auto-generated mapping extension methods for VitarishSeaDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static Dto.AttachmentReadDto ToDto(this Attachment entity) => Mapper.ToDto(entity);

    public static Attachment ToDomain(this Dto.AttachmentWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.AttachmentWriteDto ToWrite(this Dto.AttachmentReadDto dto) => Mapper.ToWrite(dto);

    public static AttachmentDescriptor ToDomain(this Dto.AttachmentDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.EventReadDto ToDto(this Event entity) => Mapper.ToDto(entity);

    public static Event ToDomain(this Dto.EventWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.EventWriteDto ToWrite(this Dto.EventReadDto dto) => Mapper.ToWrite(dto);

    public static EventDescriptor ToDomain(this Dto.EventDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.EventRoleReadDto ToDto(this EventRole entity) => Mapper.ToDto(entity);

    public static EventRole ToDomain(this Dto.EventRoleWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.EventRoleWriteDto ToWrite(this Dto.EventRoleReadDto dto) => Mapper.ToWrite(dto);

    public static EventRoleDescriptor ToDomain(this Dto.EventRoleDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.ProcedureReadDto ToDto(this Procedure entity) => Mapper.ToDto(entity);

    public static Procedure ToDomain(this Dto.ProcedureWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.ProcedureWriteDto ToWrite(this Dto.ProcedureReadDto dto) => Mapper.ToWrite(dto);

    public static ProcedureDescriptor ToDomain(this Dto.ProcedureDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.ProjectReadDto ToDto(this Project entity) => Mapper.ToDto(entity);

    public static Project ToDomain(this Dto.ProjectWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.ProjectWriteDto ToWrite(this Dto.ProjectReadDto dto) => Mapper.ToWrite(dto);

    public static ProjectDescriptor ToDomain(this Dto.ProjectDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.ReceiptReadDto ToDto(this Receipt entity) => Mapper.ToDto(entity);

    public static Receipt ToDomain(this Dto.ReceiptWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.ReceiptWriteDto ToWrite(this Dto.ReceiptReadDto dto) => Mapper.ToWrite(dto);

    public static ReceiptDescriptor ToDomain(this Dto.ReceiptDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.RoleReadDto ToDto(this Role entity) => Mapper.ToDto(entity);

    public static Role ToDomain(this Dto.RoleWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.RoleWriteDto ToWrite(this Dto.RoleReadDto dto) => Mapper.ToWrite(dto);

    public static RoleDescriptor ToDomain(this Dto.RoleDescriptorDto dto) => Mapper.ToDomain(dto);

    public static Dto.UserRoleReadDto ToDto(this UserRole entity) => Mapper.ToDto(entity);

    public static UserRole ToDomain(this Dto.UserRoleWriteDto dto) => Mapper.ToDomain(dto);

    public static Dto.UserRoleWriteDto ToWrite(this Dto.UserRoleReadDto dto) => Mapper.ToWrite(dto);

    public static UserRoleDescriptor ToDomain(this Dto.UserRoleDescriptorDto dto) => Mapper.ToDomain(dto);

}
