using Vorn.Entities;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

public interface IFileInformationService : IEntityService<FileInformationDto, FileInformationDescriptorDto>
{
}
