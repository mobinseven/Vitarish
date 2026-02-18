namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Entities.Server.Abstractions;
using Vitarish.Sea.Domain.Dto;

public interface IProceduresService : IEntityService<ProcedureReadDto, ProcedureWriteDto, ProcedureDescriptorDto>
{
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
