namespace Vitarish.Contents.Domain.Mappers;

public static class ContentsEntityMapperExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static ContentItemReadDto ToRead(this ContentItem src) => Mapper.ToRead(src);
    public static ContentItem ToEntity(this ContentItemWriteDto src) => Mapper.ToEntity(src);
    public static ContentItemDescriptor ToDescriptor(this ContentItemDescriptorDto src) => Mapper.ToDescriptor(src);

    public static TermReadDto ToRead(this Term src) => Mapper.ToRead(src);
    public static Term ToEntity(this TermWriteDto src) => Mapper.ToEntity(src);
    public static TermDescriptor ToDescriptor(this TermDescriptorDto src) => Mapper.ToDescriptor(src);

    public static ContentTermReadDto ToRead(this ContentTerm src) => Mapper.ToRead(src);
    public static ContentTerm ToEntity(this ContentTermWriteDto src) => Mapper.ToEntity(src);
    public static ContentTermDescriptor ToDescriptor(this ContentTermDescriptorDto src) => Mapper.ToDescriptor(src);

    public static ContentRelationReadDto ToRead(this ContentRelation src) => Mapper.ToRead(src);
    public static ContentRelation ToEntity(this ContentRelationWriteDto src) => Mapper.ToEntity(src);
    public static ContentRelationDescriptor ToDescriptor(this ContentRelationDescriptorDto src) => Mapper.ToDescriptor(src);

    public static AssetReadDto ToRead(this Asset src) => Mapper.ToRead(src);
    public static Asset ToEntity(this AssetWriteDto src) => Mapper.ToEntity(src);
    public static AssetDescriptor ToDescriptor(this AssetDescriptorDto src) => Mapper.ToDescriptor(src);

    public static ContentAssetReadDto ToRead(this ContentAsset src) => Mapper.ToRead(src);
    public static ContentAsset ToEntity(this ContentAssetWriteDto src) => Mapper.ToEntity(src);
    public static ContentAssetDescriptor ToDescriptor(this ContentAssetDescriptorDto src) => Mapper.ToDescriptor(src);

    public static ContentRevisionReadDto ToRead(this ContentRevision src) => Mapper.ToRead(src);
    public static ContentRevision ToEntity(this ContentRevisionWriteDto src) => Mapper.ToEntity(src);
    public static ContentRevisionDescriptor ToDescriptor(this ContentRevisionDescriptorDto src) => Mapper.ToDescriptor(src);
}
