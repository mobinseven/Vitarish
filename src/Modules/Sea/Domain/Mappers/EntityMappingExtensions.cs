namespace Vitarish.Sea.Domain.Mappers;


/// <summary>
/// Auto-generated mapping extension methods for VitarishSeaDomain entities.
/// These methods provide convenient mapping between Entity types and their DTOs.
/// </summary>
public static partial class EntityMappingExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static Dto.AttachmentReadDto ToRead(this Attachment entity) => Mapper.ToRead(entity);

    public static Attachment ToEntity(this Dto.AttachmentWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.AttachmentWriteDto ToWrite(this Dto.AttachmentReadDto dto) => Mapper.ToWrite(dto);

    public static AttachmentDescriptor ToDescriptor(this Dto.AttachmentDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.EventReadDto ToRead(this Event entity) => Mapper.ToRead(entity);

    public static Event ToEntity(this Dto.EventWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.EventWriteDto ToWrite(this Dto.EventReadDto dto) => Mapper.ToWrite(dto);

    public static EventDescriptor ToDescriptor(this Dto.EventDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.EventRoleReadDto ToRead(this EventRole entity) => Mapper.ToRead(entity);

    public static EventRole ToEntity(this Dto.EventRoleWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.EventRoleWriteDto ToWrite(this Dto.EventRoleReadDto dto) => Mapper.ToWrite(dto);

    public static EventRoleDescriptor ToDescriptor(this Dto.EventRoleDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ProcedureReadDto ToRead(this Procedure entity) => Mapper.ToRead(entity);

    public static Procedure ToEntity(this Dto.ProcedureWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ProcedureWriteDto ToWrite(this Dto.ProcedureReadDto dto) => Mapper.ToWrite(dto);

    public static ProcedureDescriptor ToDescriptor(this Dto.ProcedureDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ProjectReadDto ToRead(this Project entity) => Mapper.ToRead(entity);

    public static Project ToEntity(this Dto.ProjectWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ProjectWriteDto ToWrite(this Dto.ProjectReadDto dto) => Mapper.ToWrite(dto);

    public static ProjectDescriptor ToDescriptor(this Dto.ProjectDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.ReceiptReadDto ToRead(this Receipt entity) => Mapper.ToRead(entity);

    public static Receipt ToEntity(this Dto.ReceiptWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.ReceiptWriteDto ToWrite(this Dto.ReceiptReadDto dto) => Mapper.ToWrite(dto);

    public static ReceiptDescriptor ToDescriptor(this Dto.ReceiptDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.RoleReadDto ToRead(this Role entity) => Mapper.ToRead(entity);

    public static Role ToEntity(this Dto.RoleWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.RoleWriteDto ToWrite(this Dto.RoleReadDto dto) => Mapper.ToWrite(dto);

    public static RoleDescriptor ToDescriptor(this Dto.RoleDescriptorDto dto) => Mapper.ToDescriptor(dto);

    public static Dto.UserRoleReadDto ToRead(this UserRole entity) => Mapper.ToRead(entity);

    public static UserRole ToEntity(this Dto.UserRoleWriteDto dto) => Mapper.ToEntity(dto);

    public static Dto.UserRoleWriteDto ToWrite(this Dto.UserRoleReadDto dto) => Mapper.ToWrite(dto);

    public static UserRoleDescriptor ToDescriptor(this Dto.UserRoleDescriptorDto dto) => Mapper.ToDescriptor(dto);

}
