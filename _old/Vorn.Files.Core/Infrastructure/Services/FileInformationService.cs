using Vorn.Files.Core.Presentation.Interfaces;

namespace Vorn.Files.Core.Infrastructure.Services;

public class FileInformationService(IMediator mediator, FileInformationMapper mapper)
    : EntityService<FileInformation, FileInformationDescriptor, FileInformationDto, FileInformationDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain),
    IFileInformationService
{

}
