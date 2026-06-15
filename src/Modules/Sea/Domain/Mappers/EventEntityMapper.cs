
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Event entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.EventReadDto ToRead(global::Vitarish.Sea.Domain.Entities.Event src);

    public partial global::Vitarish.Sea.Domain.Entities.Event ToEntity(global::Vitarish.Sea.Domain.Dto.EventWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.EventWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.EventReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.EventDescriptor ToDescriptor(global::Vitarish.Sea.Domain.Dto.EventDescriptorDto src);
}
