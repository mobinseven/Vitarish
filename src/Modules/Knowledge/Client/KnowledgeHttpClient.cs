using System.Net;
using Vitarish.Knowledge.Contracts.Errors;
using Vitarish.Knowledge.Contracts.Models;
using Vitarish.Knowledge.Contracts.Requests;
using Vitarish.Knowledge.Contracts.Responses;
using Vitarish.Knowledge.Contracts.Results;
using Vitarish.Knowledge.Contracts.Routes;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Knowledge.Client;

public sealed class KnowledgeHttpClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IKnowledgeClient
{
    public Task<KnowledgeResult<KnowledgeOwnerCollectionModel>> GetOwnerWorkspaceAsync(KnowledgeOwnerWorkspaceRequest request, CancellationToken cancellationToken) => Post<KnowledgeOwnerWorkspaceRequest, KnowledgeOwnerCollectionModel>(KnowledgeApiRoutes.OwnerWorkspace, request, cancellationToken);
    public Task<KnowledgeResult<CreateKnowledgeCorpusResponse>> CreateCorpusAsync(CreateKnowledgeCorpusRequest request, CancellationToken cancellationToken) => Post<CreateKnowledgeCorpusRequest, CreateKnowledgeCorpusResponse>(KnowledgeApiRoutes.Corpora, request, cancellationToken);
    public Task<KnowledgeResult<ImportKnowledgeSourceResponse>> ImportSourceAsync(ImportKnowledgeSourceRequest request, CancellationToken cancellationToken) => Post<ImportKnowledgeSourceRequest, ImportKnowledgeSourceResponse>(KnowledgeApiRoutes.Sources, request, cancellationToken);
    public Task<KnowledgeResult<ImportKnowledgeSourceResponse>> ImportSourceFromFileAsync(ImportKnowledgeSourceFromFileRequest request, CancellationToken cancellationToken) => Post<ImportKnowledgeSourceFromFileRequest, ImportKnowledgeSourceResponse>(KnowledgeApiRoutes.SourceFiles, request, cancellationToken);
    public Task<KnowledgeResult<ReplaceKnowledgeSourceResponse>> ReplaceSourceAsync(ReplaceKnowledgeSourceRequest request, CancellationToken cancellationToken) => Post<ReplaceKnowledgeSourceRequest, ReplaceKnowledgeSourceResponse>(KnowledgeApiRoutes.ReplaceSource, request, cancellationToken);
    public Task<KnowledgeResult<ReplaceKnowledgeSourceResponse>> ReplaceSourceFromFileAsync(ReplaceKnowledgeSourceFromFileRequest request, CancellationToken cancellationToken) => Post<ReplaceKnowledgeSourceFromFileRequest, ReplaceKnowledgeSourceResponse>(KnowledgeApiRoutes.ReplaceSourceFile, request, cancellationToken);
    public Task<KnowledgeResult<DeleteKnowledgeSourceResponse>> DeleteSourceAsync(DeleteKnowledgeSourceRequest request, CancellationToken cancellationToken) => Post<DeleteKnowledgeSourceRequest, DeleteKnowledgeSourceResponse>(KnowledgeApiRoutes.DeleteSource, request, cancellationToken);
    public Task<KnowledgeResult<IndexKnowledgeCorpusResponse>> IndexCorpusAsync(IndexKnowledgeCorpusRequest request, CancellationToken cancellationToken) => Post<IndexKnowledgeCorpusRequest, IndexKnowledgeCorpusResponse>(KnowledgeApiRoutes.Indexing, request, cancellationToken);
    public Task<KnowledgeResult<KnowledgeAnswerModel>> AskAsync(AskKnowledgeRequest request, CancellationToken cancellationToken) => Post<AskKnowledgeRequest, KnowledgeAnswerModel>(KnowledgeApiRoutes.Ask, request, cancellationToken);
    public Task<KnowledgeResult<EvaluateKnowledgeCorpusResponse>> EvaluateCorpusAsync(EvaluateKnowledgeCorpusRequest request, CancellationToken cancellationToken) => Post<EvaluateKnowledgeCorpusRequest, EvaluateKnowledgeCorpusResponse>(KnowledgeApiRoutes.Evaluation, request, cancellationToken);
    public Task<KnowledgeResult<IReadOnlyList<KnowledgeSourceDiagnosticsModel>>> LoadSourceDiagnosticsAsync(KnowledgeCorpusDiagnosticsRequest request, CancellationToken cancellationToken) => Post<KnowledgeCorpusDiagnosticsRequest, IReadOnlyList<KnowledgeSourceDiagnosticsModel>>(KnowledgeApiRoutes.DiagnosticsSources, request, cancellationToken);
    public Task<KnowledgeResult<IReadOnlyList<KnowledgeChunkDiagnosticsModel>>> LoadChunkDiagnosticsAsync(KnowledgeChunkDiagnosticsRequest request, CancellationToken cancellationToken) => Post<KnowledgeChunkDiagnosticsRequest, IReadOnlyList<KnowledgeChunkDiagnosticsModel>>(KnowledgeApiRoutes.DiagnosticsChunks, request, cancellationToken);
    public Task<KnowledgeResult<IReadOnlyList<KnowledgeAskTraceDiagnosticsModel>>> LoadAskTraceDiagnosticsAsync(KnowledgeAskTraceDiagnosticsRequest request, CancellationToken cancellationToken) => Post<KnowledgeAskTraceDiagnosticsRequest, IReadOnlyList<KnowledgeAskTraceDiagnosticsModel>>(KnowledgeApiRoutes.DiagnosticsAskTraces, request, cancellationToken);
    public Task<KnowledgeResult<KnowledgePipelineDiagnosticsModel>> LoadAskDiagnosticsAsync(KnowledgeDiagnosticsRequest request, CancellationToken cancellationToken) => Post<KnowledgeDiagnosticsRequest, KnowledgePipelineDiagnosticsModel>(KnowledgeApiRoutes.DiagnosticsInspect, request, cancellationToken);
    public Task<KnowledgeResult<KnowledgeAskDebugModel>> DebugAskAsync(AskKnowledgeRequest request, CancellationToken cancellationToken) => Post<AskKnowledgeRequest, KnowledgeAskDebugModel>(KnowledgeApiRoutes.DiagnosticsDebugAsk, request, cancellationToken);
    private async Task<KnowledgeResult<TResponse>> Post<TRequest,TResponse>(string route,TRequest request,CancellationToken token)=>Map(await PostJsonAsync<TRequest,TResponse>(KnowledgeApiRoutes.Base+route,request,token));
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    private static KnowledgeResult<T> Map<T>(ModuleApiClientResult<T> result)=> result.IsSuccess ? KnowledgeResult<T>.Success(result.Value!) : KnowledgeResult<T>.Failed(MapFailure(result.Failure!));
    private static KnowledgeContractError MapFailure(ModuleApiClientFailure failure)=>new(MapCode(failure), failure.Message, null, failure.StatusCode);
    private static KnowledgeErrorCode MapCode(ModuleApiClientFailure failure)=> failure.Code switch
    {
        "Canceled" => KnowledgeErrorCode.Cancellation, "Timeout" => KnowledgeErrorCode.Timeout, "Transport" => KnowledgeErrorCode.TransportFailure, "Deserialization" => KnowledgeErrorCode.DeserializationFailure,
        nameof(HttpStatusCode.BadRequest) => KnowledgeErrorCode.ValidationFailure, nameof(HttpStatusCode.Unauthorized)=>KnowledgeErrorCode.AuthorizationFailure, nameof(HttpStatusCode.Forbidden)=>KnowledgeErrorCode.Forbidden, nameof(HttpStatusCode.NotFound)=>KnowledgeErrorCode.NotFound, nameof(HttpStatusCode.Conflict)=>KnowledgeErrorCode.Conflict,
        nameof(HttpStatusCode.UnsupportedMediaType)=>KnowledgeErrorCode.UnsupportedFileType, nameof(HttpStatusCode.RequestTimeout)=>KnowledgeErrorCode.Timeout, nameof(HttpStatusCode.GatewayTimeout)=>KnowledgeErrorCode.Timeout, nameof(HttpStatusCode.ServiceUnavailable)=>KnowledgeErrorCode.ProviderFailure, _=>KnowledgeErrorCode.UnexpectedFailure
    };
}
