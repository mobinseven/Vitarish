using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Queries;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeDiagnosticsService
{
    Task<IReadOnlyList<KnowledgeCorpusDiagnostics>> GetCorporaDiagnosticsAsync(CancellationToken cancellationToken);
    Task<KnowledgeCorpusDiagnostics> GetCorpusDiagnosticsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSourceDiagnostics>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunkDiagnostics>> GetChunksAsync(Guid corpusId, string tenantId, KnowledgeChunkDiagnosticsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeAskTraceDiagnostics>> GetAskTracesAsync(Guid corpusId, string tenantId, KnowledgeAskTraceDiagnosticsQuery query, CancellationToken cancellationToken);
    Task<KnowledgePipelineDiagnostics> InspectAsync(KnowledgeDiagnosticsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeAskDebugResult> DebugAskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeIndexDiagnostics> GetIndexDiagnosticsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
}
