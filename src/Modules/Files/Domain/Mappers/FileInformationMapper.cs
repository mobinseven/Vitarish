using Riok.Mapperly.Abstractions;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class FileInformationMapper
{
    [MapProperty(nameof(FileInformation.Metadata), nameof(FileInformationDto.MetadataJson))]
    [MapProperty(nameof(FileInformation.TenantId), nameof(FileInformationDto.Owner))]
    [MapperIgnoreTarget(nameof(FileInformationDto.Metadata))]
    [MapperIgnoreSource(nameof(FileInformation.Extension))]
    [MapperIgnoreSource(nameof(FileInformation.Size))]
    [MapperIgnoreSource(nameof(FileInformation.Owner))]
    public partial FileInformationDto ToDto(FileInformation src);

    [MapProperty(nameof(FileInformationDto.MetadataJson), nameof(FileInformation.Metadata))]
    [MapperIgnoreSource(nameof(FileInformationDto.Metadata))]
    public partial FileInformation ToDomain(FileInformationDto src);

    [MapProperty(nameof(FileInformationWriteDto.Owner), nameof(FileInformation.TenantId))]
    public partial FileInformation ToDomain(FileInformationWriteDto src);

    [MapperIgnoreSource(nameof(FileInformationDescriptorDto.AdditionalData))]
    public partial FileInformationDescriptor ToDomain(FileInformationDescriptorDto src);
}

public static class FilesEntityMapperExtensions
{
    private static FileInformationMapper Mapper = new FileInformationMapper();
    public static FileInformationDto ToDto(this FileInformation src)
    {
        return Mapper.ToDto(src);
    }
    public static FileInformation ToDomain(this FileInformationWriteDto src)
    {
        return Mapper.ToDomain(src);
    }
    public static FileInformationDescriptor ToDescriptor(this FileInformationDescriptorDto src)
    {
        return Mapper.ToDomain(src);
    }
}
