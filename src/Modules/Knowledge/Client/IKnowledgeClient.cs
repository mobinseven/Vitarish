using Vitarish.Knowledge.Contracts.Models;
using Vitarish.Knowledge.Contracts.Requests;
using Vitarish.Knowledge.Contracts.Responses;
using Vitarish.Knowledge.Contracts.Results;

namespace Vitarish.Knowledge.Client;

public interface IKnowledgeClient
{
    Task<KnowledgeResult<KnowledgeOwnerCollectionModel>> GetOwnerWorkspaceAsync(KnowledgeOwnerWorkspaceRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<CreateKnowledgeCorpusResponse>> CreateCorpusAsync(CreateKnowledgeCorpusRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<ImportKnowledgeSourceResponse>> ImportSourceAsync(ImportKnowledgeSourceRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<ImportKnowledgeSourceResponse>> ImportSourceFromFileAsync(ImportKnowledgeSourceFromFileRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<ReplaceKnowledgeSourceResponse>> ReplaceSourceAsync(ReplaceKnowledgeSourceRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<ReplaceKnowledgeSourceResponse>> ReplaceSourceFromFileAsync(ReplaceKnowledgeSourceFromFileRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<DeleteKnowledgeSourceResponse>> DeleteSourceAsync(DeleteKnowledgeSourceRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<IndexKnowledgeCorpusResponse>> IndexCorpusAsync(IndexKnowledgeCorpusRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<KnowledgeAnswerModel>> AskAsync(AskKnowledgeRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<EvaluateKnowledgeCorpusResponse>> EvaluateCorpusAsync(EvaluateKnowledgeCorpusRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<IReadOnlyList<KnowledgeSourceDiagnosticsModel>>> LoadSourceDiagnosticsAsync(KnowledgeCorpusDiagnosticsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<IReadOnlyList<KnowledgeChunkDiagnosticsModel>>> LoadChunkDiagnosticsAsync(KnowledgeChunkDiagnosticsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<IReadOnlyList<KnowledgeAskTraceDiagnosticsModel>>> LoadAskTraceDiagnosticsAsync(KnowledgeAskTraceDiagnosticsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<KnowledgePipelineDiagnosticsModel>> LoadAskDiagnosticsAsync(KnowledgeDiagnosticsRequest request, CancellationToken cancellationToken);
    Task<KnowledgeResult<KnowledgeAskDebugModel>> DebugAskAsync(AskKnowledgeRequest request, CancellationToken cancellationToken);
}
