
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Role entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.RoleReadDto ToRead(global::Vitarish.Sea.Domain.Entities.Role src);

    public partial global::Vitarish.Sea.Domain.Entities.Role ToEntity(global::Vitarish.Sea.Domain.Dto.RoleWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.RoleWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.RoleReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.RoleDescriptor ToDescriptor(global::Vitarish.Sea.Domain.Dto.RoleDescriptorDto src);
}
