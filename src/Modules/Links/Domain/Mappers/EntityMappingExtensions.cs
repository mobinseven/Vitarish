namespace Vitarish.Links.Domain.Mappers;

public static class LinksEntityMapperExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static LinkReadDto ToRead(this Link src) => Mapper.ToRead(src);
    public static Link ToEntity(this LinkWriteDto src) => Mapper.ToEntity(src);
    public static LinkWriteDto ToWrite(this LinkReadDto src) => Mapper.ToWrite(src);
    public static LinkDescriptor ToDescriptor(this LinkDescriptorDto src) => Mapper.ToDescriptor(src);
}
