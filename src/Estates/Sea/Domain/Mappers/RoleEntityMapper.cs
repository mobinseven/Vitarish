
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Role entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.RoleReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Role src);

    public partial global::Vitarish.Sea.Domain.Entities.Role ToDomain(global::Vitarish.Sea.Domain.Dto.RoleWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.RoleWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.RoleReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.RoleDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.RoleDescriptorDto src);
}
