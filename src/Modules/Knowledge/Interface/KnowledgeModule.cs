using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Repositories;
using Vitarish.Modules.Base;
using Vitarish.Knowledge.Infrastructure.Services.Answering;
using Vitarish.Knowledge.Infrastructure.Services.Evaluation;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Knowledge.Infrastructure.Services;
using Vitarish.Knowledge.Infrastructure.Services.Indexing;
using Vitarish.Knowledge.Infrastructure.Services.Importing;
using Vitarish.Knowledge.Infrastructure.Services.Diagnostics;
using Vitarish.Knowledge.Infrastructure.Services.Chunking;
using Vitarish.Search.Application.Abstractions;

namespace Vitarish.Knowledge.Interface;

public sealed class KnowledgeModule : Module, IKnowledgeModule
{
    public override string Name => "Knowledge";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IKnowledgeModule, KnowledgeModule>();
        _ = configureDbContext is null
            ? builder.Services.AddDbContext<KnowledgeDbContext>()
            : builder.Services.AddDbContext<KnowledgeDbContext>(configureDbContext);
        _ = builder.Services.AddScoped<IKnowledgeChunker, HeadingAwareKnowledgeChunker>();
        _ = builder.Services.AddScoped<IKnowledgeImporter, KnowledgeImporter>();
        _ = builder.Services.AddScoped<IKnowledgeIndexer, SearchKnowledgeIndexer>();
        _ = builder.Services.AddScoped<ISearchIndexProvider, KnowledgeSearchIndexProvider>();
        _ = builder.Services.AddScoped<IKnowledgeRetriever, SearchKnowledgeRetriever>();
        _ = builder.Services.AddScoped<IKnowledgeContextBuilder, KnowledgeContextBuilder>();
        _ = builder.Services.AddScoped<IKnowledgeAnswerer, IntelligenceKnowledgeAnswerer>();
        _ = builder.Services.AddScoped<IKnowledgeEvaluator, KnowledgeEvaluator>();
        _ = builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        _ = builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
        _ = builder.Services.AddScoped<IKnowledgeDiagnosticsService, KnowledgeDiagnosticsService>();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        RouteGroupBuilder diagnostics = app.MapGroup("/owner/knowledge");
        _ = diagnostics.MapGet("/corpora", GetCorporaDiagnosticsAsync);
        _ = diagnostics.MapGet("/{tenantId}/{corpusId}", GetCorpusDiagnosticsAsync);
        _ = diagnostics.MapGet("/{tenantId}/{corpusId}/sources", GetSourceDiagnosticsAsync);
        _ = diagnostics.MapGet("/{tenantId}/{corpusId}/chunks", GetChunkDiagnosticsAsync);
        _ = diagnostics.MapGet("/{tenantId}/{corpusId}/traces", GetTraceDiagnosticsAsync);
        _ = diagnostics.MapGet("/{tenantId}/{corpusId}/index", GetIndexDiagnosticsAsync);
        _ = diagnostics.MapPost("/{tenantId}/{corpusId}/inspect", InspectAsync);
        _ = diagnostics.MapPost("/{tenantId}/{corpusId}/debug-ask", DebugAskAsync);
        return Task.CompletedTask;
    }

    private static Task<IReadOnlyList<KnowledgeCorpusDiagnostics>> GetCorporaDiagnosticsAsync(IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.GetCorporaDiagnosticsAsync(cancellationToken);

    private static Task<KnowledgeCorpusDiagnostics> GetCorpusDiagnosticsAsync(string tenantId, string corpusId, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.GetCorpusDiagnosticsAsync(corpusId, tenantId, cancellationToken);

    private static Task<IReadOnlyList<KnowledgeSourceDiagnostics>> GetSourceDiagnosticsAsync(string tenantId, string corpusId, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.GetSourcesAsync(corpusId, tenantId, cancellationToken);

    private static Task<IReadOnlyList<KnowledgeChunkDiagnostics>> GetChunkDiagnosticsAsync(string tenantId, string corpusId, [AsParameters] KnowledgeChunkDiagnosticsQuery query, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.GetChunksAsync(corpusId, tenantId, query, cancellationToken);

    private static Task<IReadOnlyList<KnowledgeAskTraceDiagnostics>> GetTraceDiagnosticsAsync(string tenantId, string corpusId, [AsParameters] KnowledgeAskTraceDiagnosticsQuery query, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.GetAskTracesAsync(corpusId, tenantId, query, cancellationToken);

    private static Task<KnowledgeIndexDiagnostics> GetIndexDiagnosticsAsync(string tenantId, string corpusId, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.GetIndexDiagnosticsAsync(corpusId, tenantId, cancellationToken);

    private static Task<KnowledgePipelineDiagnostics> InspectAsync(string tenantId, string corpusId, KnowledgeDiagnosticsRequest request, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.InspectAsync(request with { TenantId = tenantId, CorpusId = corpusId }, cancellationToken);

    private static Task<KnowledgeAskDebugResult> DebugAskAsync(string tenantId, string corpusId, KnowledgeAskRequest request, IKnowledgeDiagnosticsService diagnostics, CancellationToken cancellationToken) =>
        diagnostics.DebugAskAsync(request with { TenantId = tenantId, CorpusId = corpusId, SaveTrace = true, TraceKind = request.TraceKind ?? "diagnostic" }, cancellationToken);
}
