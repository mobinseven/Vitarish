using Riok.Mapperly.Abstractions;
using Vitarish.Entities.Contracts.Models;

[assembly: Vitarish.Entities.Generators.GenerateEntitiesRegistrationAttribute]

namespace Vitarish.Contents.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class EntityMapper
{
    public partial ContentItem ToEntity(ContentItemWriteDto src);
    // The legacy item contract deliberately excludes structured authoring state.
    [MapperIgnoreSource(nameof(ContentItem.SiteId))]
    [MapperIgnoreSource(nameof(ContentItem.ContentTypeId))]
    [MapperIgnoreSource(nameof(ContentItem.SchemaVersion))]
    [MapperIgnoreSource(nameof(ContentItem.Fields))]
    [MapperIgnoreSource(nameof(ContentItem.WorkingRevisionId))]
    [MapperIgnoreSource(nameof(ContentItem.PublishedRevisionId))]
    [MapperIgnoreSource(nameof(ContentItem.Version))]
    public partial ContentItemReadDto ToRead(ContentItem src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentItemDescriptor ToDescriptor(ContentItemDescriptorDto src);

    public partial Term ToEntity(TermWriteDto src);
    public partial TermReadDto ToRead(Term src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial TermDescriptor ToDescriptor(TermDescriptorDto src);

    public partial ContentTerm ToEntity(ContentTermWriteDto src);
    public partial ContentTermReadDto ToRead(ContentTerm src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentTermDescriptor ToDescriptor(ContentTermDescriptorDto src);

    public partial ContentRelation ToEntity(ContentRelationWriteDto src);
    public partial ContentRelationReadDto ToRead(ContentRelation src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentRelationDescriptor ToDescriptor(ContentRelationDescriptorDto src);

    public partial Asset ToEntity(AssetWriteDto src);
    public partial AssetReadDto ToRead(Asset src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial AssetDescriptor ToDescriptor(AssetDescriptorDto src);

    public partial ContentAsset ToEntity(ContentAssetWriteDto src);
    public partial ContentAssetReadDto ToRead(ContentAsset src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentAssetDescriptor ToDescriptor(ContentAssetDescriptorDto src);

    public partial ContentRevision ToEntity(ContentRevisionWriteDto src);
    // Structured revision snapshots are exposed by the new authoring contract.
    [MapperIgnoreSource(nameof(ContentRevision.Fields))]
    [MapperIgnoreSource(nameof(ContentRevision.Title))]
    [MapperIgnoreSource(nameof(ContentRevision.Slug))]
    [MapperIgnoreSource(nameof(ContentRevision.CanonicalPath))]
    [MapperIgnoreSource(nameof(ContentRevision.Excerpt))]
    [MapperIgnoreSource(nameof(ContentRevision.SchemaVersion))]
    public partial ContentRevisionReadDto ToRead(ContentRevision src);

    [MapperIgnoreSource(nameof(EntityDescriptorDto.AdditionalData))]
    public partial ContentRevisionDescriptor ToDescriptor(ContentRevisionDescriptorDto src);
}
