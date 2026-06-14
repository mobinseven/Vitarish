using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.CommandHandlers;

public sealed class KnowledgeImportCommandHandler(IKnowledgeImportPersistenceRepository importRepository) : IKnowledgeImportCommandHandler
{
    public Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken)
        => importRepository.SaveImportAsync(result, cancellationToken);

    public Task ReplaceSourceAsync(Guid sourceId, KnowledgeImportResult result, CancellationToken cancellationToken)
        => importRepository.ReplaceSourceAsync(sourceId, result, cancellationToken);
}
