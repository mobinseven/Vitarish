using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Mappers;

namespace Vitarish.Files.Infrastructure.Services;

public class FileInformationService(IMediator mediator)
    : EntityService<FileInformation, FileInformationDescriptor, FileInformationDto, FileInformationWriteDto, FileInformationDescriptorDto>(
        mediator,
        FilesEntityMapperExtensions.ToDto,
        FilesEntityMapperExtensions.ToDomain,
        FilesEntityMapperExtensions.ToDescriptor),
    IFileInformationService
{

}
