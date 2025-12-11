using Vitarish.Files.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public class FileInformationService(IMediator mediator, FileInformationMapper mapper)
    : EntityService<FileInformation, FileInformationDescriptor, FileInformationDto, FileInformationDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain),
    IFileInformationService
{

}
