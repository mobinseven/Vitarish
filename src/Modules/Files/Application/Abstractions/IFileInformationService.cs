using Vitarish.Entities.Server.Abstractions;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

public interface IFileInformationService : IEntityService<FileInformationReadDto, FileInformationWriteDto, FileInformationDescriptorDto>
{
}
