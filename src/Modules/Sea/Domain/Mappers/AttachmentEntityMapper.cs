
namespace Vitarish.Sea.Domain.Mappers;

/// <summary>
/// Auto-generated mapper for Attachment entity using Riok.Mapperly source generation.
/// </summary>
public partial class EntityMapper
{
    public partial global::Vitarish.Sea.Domain.Dto.AttachmentReadDto ToRead(global::Vitarish.Sea.Domain.Entities.Attachment src);

    public partial global::Vitarish.Sea.Domain.Entities.Attachment ToEntity(global::Vitarish.Sea.Domain.Dto.AttachmentWriteDto src);

    public partial global::Vitarish.Sea.Domain.Dto.AttachmentWriteDto ToWrite(global::Vitarish.Sea.Domain.Dto.AttachmentReadDto src);

    public partial global::Vitarish.Sea.Domain.Models.AttachmentDescriptor ToDescriptor(global::Vitarish.Sea.Domain.Dto.AttachmentDescriptorDto src);
}
