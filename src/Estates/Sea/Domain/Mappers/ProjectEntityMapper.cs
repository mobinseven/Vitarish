
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Project entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.ProjectReadDto ToDto(global::Vitarish.Sea.Domain.Entities.Project src);

    public partial global::Vitarish.Sea.Domain.Entities.Project ToDomain(global::Vitarish.Sea.Domain.Dto.ProjectWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.ProjectWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.ProjectReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.ProjectDescriptor ToDomain(global::Vitarish.Sea.Domain.Dto.ProjectDescriptorDto src);
}
