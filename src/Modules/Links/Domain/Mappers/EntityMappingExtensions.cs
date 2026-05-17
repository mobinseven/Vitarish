namespace Vitarish.Links.Domain.Mappers;

public static class LinksEntityMapperExtensions
{
    private static readonly EntityMapper Mapper = new();

    public static LinkReadDto ToDto(this Link src) => Mapper.ToDto(src);
    public static Link ToDomain(this LinkWriteDto src) => Mapper.ToDomain(src);
    public static LinkDescriptor ToDescriptor(this LinkDescriptorDto src) => Mapper.ToDomain(src);
}
