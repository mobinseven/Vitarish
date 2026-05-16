using Riok.Mapperly.Abstractions;

[assembly: Vitarish.Entities.Generators.GenerateEntitiesRegistrationAttribute]

namespace Vitarish.Contents.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
    public partial ContentItem ToDomain(ContentItemWriteDto src);
    public partial ContentItemReadDto ToDto(ContentItem src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentItemDescriptor ToDomain(ContentItemDescriptorDto src);

    public partial Term ToDomain(TermWriteDto src);
    public partial TermReadDto ToDto(Term src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial TermDescriptor ToDomain(TermDescriptorDto src);

    public partial ContentTerm ToDomain(ContentTermWriteDto src);
    public partial ContentTermReadDto ToDto(ContentTerm src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentTermDescriptor ToDomain(ContentTermDescriptorDto src);

    public partial ContentRelation ToDomain(ContentRelationWriteDto src);
    public partial ContentRelationReadDto ToDto(ContentRelation src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentRelationDescriptor ToDomain(ContentRelationDescriptorDto src);

    public partial Asset ToDomain(AssetWriteDto src);
    public partial AssetReadDto ToDto(Asset src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial AssetDescriptor ToDomain(AssetDescriptorDto src);

    public partial ContentAsset ToDomain(ContentAssetWriteDto src);
    public partial ContentAssetReadDto ToDto(ContentAsset src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentAssetDescriptor ToDomain(ContentAssetDescriptorDto src);

    public partial ContentRevision ToDomain(ContentRevisionWriteDto src);
    public partial ContentRevisionReadDto ToDto(ContentRevision src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentRevisionDescriptor ToDomain(ContentRevisionDescriptorDto src);
}
