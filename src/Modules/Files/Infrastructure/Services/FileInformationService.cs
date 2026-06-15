using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Mappers;

namespace Vitarish.Files.Infrastructure.Services;

public class FileInformationService(IMediator mediator)
    : EntityService<FileInformation, FileInformationDescriptor, FileInformationReadDto, FileInformationWriteDto, FileInformationDescriptorDto>(
        mediator,
        FilesEntityMapperExtensions.ToRead,
        FilesEntityMapperExtensions.ToEntity,
        FilesEntityMapperExtensions.ToDescriptor),
    IFileInformationService
{

}
