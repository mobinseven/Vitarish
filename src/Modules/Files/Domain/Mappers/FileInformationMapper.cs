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
    public partial FileInformationReadDto ToRead(FileInformation src);

    [MapProperty(nameof(FileInformationReadDto.MetadataJson), nameof(FileInformation.Metadata))]
    [MapperIgnoreSource(nameof(FileInformationReadDto.Metadata))]
    public partial FileInformation ToEntity(FileInformationReadDto src);

    [MapProperty(nameof(FileInformationWriteDto.Owner), nameof(FileInformation.TenantId))]
    public partial FileInformation ToEntity(FileInformationWriteDto src);
    public partial FileInformationWriteDto Clone(FileInformationWriteDto src);
    public partial FileInformationWriteDto ToWrite(FileInformationReadDto src);

    [MapperIgnoreSource(nameof(FileInformationDescriptorDto.AdditionalData))]
    public partial FileInformationDescriptor ToDescriptor(FileInformationDescriptorDto src);
}

public static class FilesEntityMapperExtensions
{
    private static readonly FileInformationMapper Mapper = new();
    public static FileInformationReadDto ToRead(this FileInformation src) => Mapper.ToRead(src);
    public static FileInformation ToEntity(this FileInformationWriteDto src) => Mapper.ToEntity(src);
    public static FileInformationWriteDto Clone(this FileInformationWriteDto src) => Mapper.Clone(src);
    public static FileInformationWriteDto ToWrite(this FileInformationReadDto src) => Mapper.ToWrite(src);
    public static FileInformationDescriptor ToDescriptor(this FileInformationDescriptorDto src) => Mapper.ToDescriptor(src);
}
