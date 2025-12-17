using Vitarish.Entities.Server.Services;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

public interface IFileInformationService : IEntityService<FileInformationDto, FileInformationWriteDto, FileInformationDescriptorDto>
{
}
