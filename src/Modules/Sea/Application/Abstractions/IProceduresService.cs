
using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Sea.Application.Abstractions;

public interface IProceduresService : IEntityService<ProcedureReadDto, ProcedureWriteDto, ProcedureDescriptorDto>
{
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
