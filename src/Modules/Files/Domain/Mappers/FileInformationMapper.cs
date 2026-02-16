using Riok.Mapperly.Abstractions;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Domain.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class FileInformationMapper
{
    [MapProperty(nameof(FileInformation.Metadata), nameof(FileInformationReadDto.MetadataJson))]
    [MapProperty(nameof(FileInformation.TenantId), nameof(FileInformationReadDto.Owner))]
    [MapperIgnoreTarget(nameof(FileInformationReadDto.Metadata))]
    [MapperIgnoreSource(nameof(FileInformation.Extension))]
    [MapperIgnoreSource(nameof(FileInformation.Size))]
    [MapperIgnoreSource(nameof(FileInformation.Owner))]
    public partial FileInformationReadDto ToDto(FileInformation src);

    [MapProperty(nameof(FileInformationReadDto.MetadataJson), nameof(FileInformation.Metadata))]
    [MapperIgnoreSource(nameof(FileInformationReadDto.Metadata))]
    public partial FileInformation ToDomain(FileInformationReadDto src);

    [MapProperty(nameof(FileInformationWriteDto.Owner), nameof(FileInformation.TenantId))]
    public partial FileInformation ToDomain(FileInformationWriteDto src);
    public partial FileInformationWriteDto Clone(FileInformationWriteDto src);

    [MapperIgnoreSource(nameof(FileInformationDescriptorDto.AdditionalData))]
    public partial FileInformationDescriptor ToDomain(FileInformationDescriptorDto src);
}

public static class FilesEntityMapperExtensions
{
    private static FileInformationMapper Mapper = new FileInformationMapper();
    public static FileInformationReadDto ToDto(this FileInformation src)
    {
        return Mapper.ToDto(src);
    }
    public static FileInformation ToDomain(this FileInformationWriteDto src)
    {
        return Mapper.ToDomain(src);
    }
    public static FileInformationWriteDto Clone(this FileInformationWriteDto src)
    {
        return Mapper.Clone(src);
    }
    public static FileInformationDescriptor ToDescriptor(this FileInformationDescriptorDto src)
    {
        return Mapper.ToDomain(src);
    }
}
