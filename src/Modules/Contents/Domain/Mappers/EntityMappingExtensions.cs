namespace Vitarish.Contents.Domain.Mappers;

public static class ContentsEntityMapperExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static ContentItemReadDto ToDto(this ContentItem src) => Mapper.ToDto(src);
    public static ContentItem ToDomain(this ContentItemWriteDto src) => Mapper.ToDomain(src);
    public static ContentItemDescriptor ToDescriptor(this ContentItemDescriptorDto src) => Mapper.ToDomain(src);

    public static TermReadDto ToDto(this Term src) => Mapper.ToDto(src);
    public static Term ToDomain(this TermWriteDto src) => Mapper.ToDomain(src);
    public static TermDescriptor ToDescriptor(this TermDescriptorDto src) => Mapper.ToDomain(src);

    public static ContentTermReadDto ToDto(this ContentTerm src) => Mapper.ToDto(src);
    public static ContentTerm ToDomain(this ContentTermWriteDto src) => Mapper.ToDomain(src);
    public static ContentTermDescriptor ToDescriptor(this ContentTermDescriptorDto src) => Mapper.ToDomain(src);

    public static ContentRelationReadDto ToDto(this ContentRelation src) => Mapper.ToDto(src);
    public static ContentRelation ToDomain(this ContentRelationWriteDto src) => Mapper.ToDomain(src);
    public static ContentRelationDescriptor ToDescriptor(this ContentRelationDescriptorDto src) => Mapper.ToDomain(src);

    public static AssetReadDto ToDto(this Asset src) => Mapper.ToDto(src);
    public static Asset ToDomain(this AssetWriteDto src) => Mapper.ToDomain(src);
    public static AssetDescriptor ToDescriptor(this AssetDescriptorDto src) => Mapper.ToDomain(src);

    public static ContentAssetReadDto ToDto(this ContentAsset src) => Mapper.ToDto(src);
    public static ContentAsset ToDomain(this ContentAssetWriteDto src) => Mapper.ToDomain(src);
    public static ContentAssetDescriptor ToDescriptor(this ContentAssetDescriptorDto src) => Mapper.ToDomain(src);

    public static ContentRevisionReadDto ToDto(this ContentRevision src) => Mapper.ToDto(src);
    public static ContentRevision ToDomain(this ContentRevisionWriteDto src) => Mapper.ToDomain(src);
    public static ContentRevisionDescriptor ToDescriptor(this ContentRevisionDescriptorDto src) => Mapper.ToDomain(src);
}
