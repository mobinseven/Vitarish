using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeImporter
{
    Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken);
    Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken);
}
