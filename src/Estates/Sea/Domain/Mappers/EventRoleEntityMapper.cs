
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for EventRole entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.EventRoleReadDto ToDto(global::Vitarish.Sea.Domain.Entities.EventRole src);

    public partial global::Vitarish.Sea.Domain.Entities.EventRole ToDomain(global::Vitarish.Sea.Domain.Dto.EventRoleWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.EventRoleWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.EventRoleReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.EventRoleDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.EventRoleDescriptorDto src);
}
