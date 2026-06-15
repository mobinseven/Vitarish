using Riok.Mapperly.Abstractions;

namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Project entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.ProjectReadDto ToRead(global::Vitarish.Sea.Domain.Entities.Project src);

    [MapperIgnoreSource("UserIds")]
    public partial global::Vitarish.Sea.Domain.Entities.Project ToEntity(global::Vitarish.Sea.Domain.Dto.ProjectWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.ProjectWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.ProjectReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.ProjectDescriptor ToDescriptor(global::Vitarish.Sea.Domain.Dto.ProjectDescriptorDto src);
}
