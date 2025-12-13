namespace Vitarish.Sea.Infrastructure.Mapping;


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterTransient]
internal partial class AttachmentsMapper
{
    public partial AttachmentDto ToDto(Attachment src);
    public partial Attachment ToDomain(AttachmentDto src);

    public partial AttachmentDescriptor ToDomain(AttachmentDescriptorDto src);
}

