using Vitarish.Files.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public class FileInformationService(IServiceScopeFactory serviceScopeFactory, FileInformationMapper mapper)
    : EntityService<FileInformation, FileInformationDescriptor, FileInformationDto, FileInformationWriteDto, FileInformationDescriptorDto>(
        serviceScopeFactory, 
        mapper.ToDto, 
        mapper.ToDomain, 
        mapper.ToDomain),
    IFileInformationService
{

}
