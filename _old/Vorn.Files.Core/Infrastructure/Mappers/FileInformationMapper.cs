namespace Vorn.Files.Core.Infrastructure.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
[RegisterSingleton]
public partial class FileInformationMapper
{
    [MapProperty(nameof(FileInformation.Metadata), nameof(FileInformationDto.MetadataJson))]
    [MapperIgnoreTarget(nameof(FileInformationDto.Metadata))]
    public partial FileInformationDto ToDto(FileInformation src);

    [MapProperty(nameof(FileInformationDto.MetadataJson), nameof(FileInformation.Metadata))]
    [MapperIgnoreSource(nameof(FileInformationDto.Metadata))]
    public partial FileInformation ToDomain(FileInformationDto src);
    public partial FileInformationDescriptor ToDomain(FileInformationDescriptorDto src);
}
