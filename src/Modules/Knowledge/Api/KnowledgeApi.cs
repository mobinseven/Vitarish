using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Queries;
using Vitarish.Knowledge.Contracts.Models;
using Vitarish.Knowledge.Contracts.Requests;
using Vitarish.Knowledge.Contracts.Responses;
using Vitarish.Knowledge.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
using AppEnums = Vitarish.Knowledge.Application.Enums;
using WireEnums = Vitarish.Knowledge.Contracts.Enums;

namespace Vitarish.Knowledge.Api;

public static class KnowledgeApi
{
    public static IEndpointRouteBuilder MapKnowledgeApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(KnowledgeApiRoutes.Base).WithTags("Knowledge");
        _ = group.MapModulePost(KnowledgeApiRoutes.OwnerWorkspace, OwnerWorkspaceAsync, "KnowledgeOwnerWorkspace");
        _ = group.MapModulePost(KnowledgeApiRoutes.Corpora, CreateCorpusAsync, "KnowledgeCreateCorpus");
        _ = group.MapModulePost(KnowledgeApiRoutes.Sources, ImportSourceAsync, "KnowledgeImportSource");
        _ = group.MapModulePost(KnowledgeApiRoutes.SourceFiles, ImportFileAsync, "KnowledgeImportFile");
        _ = group.MapModulePost(KnowledgeApiRoutes.ReplaceSource, ReplaceSourceAsync, "KnowledgeReplaceSource");
        _ = group.MapModulePost(KnowledgeApiRoutes.ReplaceSourceFile, ReplaceFileAsync, "KnowledgeReplaceFile");
        _ = group.MapModulePost(KnowledgeApiRoutes.DeleteSource, DeleteSourceAsync, "KnowledgeDeleteSource");
        _ = group.MapModulePost(KnowledgeApiRoutes.Indexing, IndexAsync, "KnowledgeIndex");
        _ = group.MapModulePost(KnowledgeApiRoutes.Ask, AskAsync, "KnowledgeAsk");
        _ = group.MapModulePost(KnowledgeApiRoutes.Evaluation, EvaluateAsync, "KnowledgeEvaluate");
        _ = group.MapModulePost(KnowledgeApiRoutes.DiagnosticsSources, SourcesDiagnosticsAsync, "KnowledgeSourcesDiagnostics");
        _ = group.MapModulePost(KnowledgeApiRoutes.DiagnosticsChunks, ChunksDiagnosticsAsync, "KnowledgeChunksDiagnostics");
        _ = group.MapModulePost(KnowledgeApiRoutes.DiagnosticsAskTraces, AskTracesDiagnosticsAsync, "KnowledgeAskTracesDiagnostics");
        _ = group.MapModulePost(KnowledgeApiRoutes.DiagnosticsInspect, InspectDiagnosticsAsync, "KnowledgeInspectDiagnostics");
        _ = group.MapModulePost(KnowledgeApiRoutes.DiagnosticsDebugAsk, DebugAskAsync, "KnowledgeDebugAsk");
        return endpoints;
    }
    private static string Tenant(IRequestContext c)=>c.TenantId ?? string.Empty;
    private static async Task<IResult> OwnerWorkspaceAsync(KnowledgeOwnerWorkspaceRequest r, IRequestContext ctx, IMediator m, CancellationToken ct)=>Results.Ok(ToModel(await m.Send(new KnowledgeOwnerCollectionQuery(Tenant(ctx), ctx.OwnerId, r.SelectedCorpusId, r.FocusedSourceId), ct)));
    private static async Task<IResult> CreateCorpusAsync(CreateKnowledgeCorpusRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new CreateKnowledgeCorpusCommand(Tenant(ctx), new CreateKnowledgeCorpusInput(r.Name,r.Description,r.IsActive)),ct);return Status(x.Succeeded,new CreateKnowledgeCorpusResponse(x.Succeeded,x.Message,x.Corpus is null?null:new Vitarish.Knowledge.Contracts.Models.KnowledgeOwnerCorpusOption(x.Corpus.Label,x.Corpus.Details ?? string.Empty){CorpusId=x.Corpus.CorpusId}));}
    private static async Task<IResult> ImportSourceAsync(ImportKnowledgeSourceRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new ImportKnowledgeSourceCommand(Tenant(ctx), new ImportKnowledgeSourceInput(r.CorpusId,r.SourceName,ToApp(r.Format),r.Content)),ct);return Status(x.Succeeded,new ImportKnowledgeSourceResponse(x.Succeeded,x.Message,x.Summary));}
    private static async Task<IResult> ImportFileAsync(ImportKnowledgeSourceFromFileRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new ImportKnowledgeSourceFromFileCommand(Tenant(ctx), new ImportKnowledgeSourceFromFileInput(r.CorpusId,r.FileInformationId,r.SourceName,ToApp(r.Format))),ct);return Status(x.Succeeded,new ImportKnowledgeSourceResponse(x.Succeeded,x.Message,x.Summary));}
    private static async Task<IResult> ReplaceSourceAsync(ReplaceKnowledgeSourceRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new ReplaceKnowledgeSourceCommand(Tenant(ctx), new ReplaceKnowledgeSourceInput(r.CorpusId,r.SourceId,r.SourceName,ToApp(r.Format),r.Content)),ct);return Status(x.Succeeded,new ReplaceKnowledgeSourceResponse(x.Succeeded,x.Message,x.Summary));}
    private static async Task<IResult> ReplaceFileAsync(ReplaceKnowledgeSourceFromFileRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new ReplaceKnowledgeSourceFromFileCommand(Tenant(ctx), new ReplaceKnowledgeSourceFromFileInput(r.CorpusId,r.SourceId,r.FileInformationId,r.SourceName,ToApp(r.Format))),ct);return Status(x.Succeeded,new ReplaceKnowledgeSourceResponse(x.Succeeded,x.Message,x.Summary));}
    private static async Task<IResult> DeleteSourceAsync(DeleteKnowledgeSourceRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new DeleteKnowledgeSourceCommand(Tenant(ctx),ctx.OwnerId,new DeleteKnowledgeSourceInput(r.CorpusId,r.SourceId)),ct);return Status(x.Succeeded,new DeleteKnowledgeSourceResponse(x.Succeeded,x.Message));}
    private static async Task<IResult> IndexAsync(IndexKnowledgeCorpusRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new IndexKnowledgeCorpusCommand(Tenant(ctx),new IndexKnowledgeCorpusInput(r.CorpusId)),ct);return Status(x.Succeeded,new IndexKnowledgeCorpusResponse(x.Succeeded,x.Message));}
    private static async Task<IResult> EvaluateAsync(EvaluateKnowledgeCorpusRequest r, IRequestContext ctx, IMediator m, CancellationToken ct){var x=await m.Send(new EvaluateKnowledgeCorpusCommand(Tenant(ctx),new EvaluateKnowledgeCorpusInput(r.CorpusId,ToApp(r.EvaluationScope))),ct);return Status(x.Succeeded,new EvaluateKnowledgeCorpusResponse(x.Succeeded,x.Message,x.Summary));}
    private static async Task<IResult> AskAsync(AskKnowledgeRequest r, IRequestContext ctx, IKnowledgeService s, CancellationToken ct)=>Results.Ok(ToModel(await s.AskAsync(new KnowledgeAskRequest{CorpusId=r.CorpusId,TenantId=Tenant(ctx)??string.Empty,Question=r.Question,TopK=r.TopK,RetrievalMode=ToApp(r.RetrievalMode),SaveTrace=r.SaveTrace,TraceKind=r.TraceKind},ct)));
    private static async Task<IResult> SourcesDiagnosticsAsync(KnowledgeCorpusDiagnosticsRequest r, IRequestContext ctx, IKnowledgeDiagnosticsService d, CancellationToken ct)=>Results.Ok((await d.GetSourcesAsync(r.CorpusId,Tenant(ctx)??string.Empty,ct)).Select(ToModel).ToArray());
    private static async Task<IResult> ChunksDiagnosticsAsync(KnowledgeChunkDiagnosticsRequest r, IRequestContext ctx, IKnowledgeDiagnosticsService d, CancellationToken ct)=>Results.Ok((await d.GetChunksAsync(r.CorpusId,Tenant(ctx)??string.Empty,new KnowledgeChunkDiagnosticsQuery{SourceId=r.SourceId,DocumentId=r.DocumentId,HeadingContains=r.HeadingContains,TextContains=r.TextContains,Limit=r.Limit},ct)).Select(ToModel).ToArray());
    private static async Task<IResult> AskTracesDiagnosticsAsync(KnowledgeAskTraceDiagnosticsRequest r, IRequestContext ctx, IKnowledgeDiagnosticsService d, CancellationToken ct)=>Results.Ok((await d.GetAskTracesAsync(r.CorpusId,Tenant(ctx)??string.Empty,new KnowledgeAskTraceDiagnosticsQuery{IsAnswered=r.IsAnswered,TraceKind=r.TraceKind,QuestionContains=r.QuestionContains,From=r.From,To=r.To,Limit=r.Limit},ct)).Select(ToModel).ToArray());
    private static async Task<IResult> InspectDiagnosticsAsync(Vitarish.Knowledge.Contracts.Requests.KnowledgeDiagnosticsRequest r, IRequestContext ctx, IKnowledgeDiagnosticsService d, CancellationToken ct)=>Results.Ok(ToModel(await d.InspectAsync(new Vitarish.Knowledge.Application.Contracts.KnowledgeDiagnosticsRequest{CorpusId=r.CorpusId,TenantId=Tenant(ctx)??string.Empty,Question=r.Question,TopK=r.TopK},ct)));
    private static async Task<IResult> DebugAskAsync(AskKnowledgeRequest r, IRequestContext ctx, IKnowledgeDiagnosticsService d, CancellationToken ct)=>Results.Ok(ToModel(await d.DebugAskAsync(new KnowledgeAskRequest{CorpusId=r.CorpusId,TenantId=Tenant(ctx)??string.Empty,Question=r.Question,TopK=r.TopK,RetrievalMode=ToApp(r.RetrievalMode),SaveTrace=r.SaveTrace,TraceKind=r.TraceKind},ct)));
    private static IResult Status<T>(bool ok,T body)=> ok?Results.Ok(body):Results.BadRequest(body);
    private static AppEnums.KnowledgeSourceInputFormat ToApp(WireEnums.KnowledgeSourceInputFormat v)=>(AppEnums.KnowledgeSourceInputFormat)(int)v; private static AppEnums.KnowledgeEvaluationScope ToApp(WireEnums.KnowledgeEvaluationScope v)=>(AppEnums.KnowledgeEvaluationScope)(int)v; private static KnowledgeRetrievalMode ToApp(WireEnums.KnowledgeRetrievalMode v)=>(KnowledgeRetrievalMode)(int)v;
    private static KnowledgeOwnerCollectionModel ToModel(KnowledgeOwnerCollectionReadModel x)=>new(x.Corpora.Select(c=>new Vitarish.Knowledge.Contracts.Models.KnowledgeOwnerCorpusCollectionItem(c.CorpusId,c.CorpusName,c.CorpusDescription,c.IsActive,c.Sources.Select(s=>new Vitarish.Knowledge.Contracts.Models.KnowledgeOwnerSourceCollectionItem(s.SourceId,s.SourceName,s.SourceType,s.ImportedAt,s.OriginalLocation,s.Files.Select(f=>new Vitarish.Knowledge.Contracts.Models.KnowledgeOwnerSourceFileCollectionItem(f.FileId,f.FileName,f.ContentType,f.Size,f.ChunkCount,f.IndexedAt)).ToArray())).ToArray())).ToArray(),x.SelectedCorpusId,x.FocusedSourceId);
    private static KnowledgeRetrievalHit ToModel(KnowledgeHit h)=>new(h.ChunkId,h.SourceId,h.DocumentId,h.Text,h.Title,h.Score); private static Vitarish.Knowledge.Contracts.Models.KnowledgeCitation ToModel(Vitarish.Knowledge.Application.Contracts.KnowledgeCitation c)=>new(c.ChunkId,c.SourceId,c.DocumentId,c.Title,c.Quote);
    private static KnowledgeAnswerModel ToModel(KnowledgeAnswer a)=>new(a.Answer,a.IsAnswered,a.Citations.Select(ToModel).ToArray(),a.MissingInformation,a.RetrievedContext.Select(ToModel).ToArray());
    private static KnowledgeSourceDiagnosticsModel ToModel(KnowledgeSourceDiagnostics s)=>new(s.SourceId,s.CorpusId,s.TenantId,s.Name,s.SourceType,s.OriginalLocation,s.Version,s.ImportedAt); private static KnowledgeChunkDiagnosticsModel ToModel(KnowledgeChunkDiagnostics c)=>new(c.ChunkId,c.SourceId,c.DocumentId,c.Title,c.HeadingPath,c.Order,c.TextLength,c.TextPreview,c.Text);
    private static KnowledgeAskTraceDiagnosticsModel ToModel(KnowledgeAskTraceDiagnostics t)=>new(t.TraceId,t.CorpusId,t.TenantId,t.Question,t.IsAnswered,t.MissingInformation,t.RetrievedChunkIds,t.RetrievedChunkIdsParseError,t.GeneratedAnswer,t.Citations.Select(ToModel).ToArray(),t.CitationsParseError,t.TraceKind,t.CreatedAt);
    private static KnowledgeCheckModel ToModel(KnowledgeCheck c)=>new(c.Passed,c.Message,c.Data); private static KnowledgePipelineDiagnosticsModel ToModel(KnowledgePipelineDiagnostics p)=>new(ToModel(p.CorpusCreated),ToModel(p.ContentImported),ToModel(p.ChunksCreated),ToModel(p.CorpusIndexed),ToModel(p.SearchRetrievedChunks),ToModel(p.ContextPackedChunks),ToModel(p.AnswerHasValidCitations),ToModel(p.RefusalIsGrounded),p.DebugAsk is null?null:ToModel(p.DebugAsk));
    private static KnowledgeAskDebugModel ToModel(KnowledgeAskDebugResult r)=>new(r.CorpusId,r.TenantId,r.Question,r.RetrievedHits.Select(ToModel).ToArray(),r.ContextChunkIds,ToModel(r.Answer),(WireEnums.KnowledgeFailureStage)(int)r.FailureStage,r.FailureReason);
}
