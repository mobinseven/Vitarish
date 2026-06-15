
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for UserRole entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.UserRoleReadDto ToRead(global::Vitarish.Sea.Domain.Entities.UserRole src);

    public partial global::Vitarish.Sea.Domain.Entities.UserRole ToEntity(global::Vitarish.Sea.Domain.Dto.UserRoleWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.UserRoleWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.UserRoleReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.UserRoleDescriptor ToDescriptor(global::Vitarish.Sea.Domain.Dto.UserRoleDescriptorDto src);
}
