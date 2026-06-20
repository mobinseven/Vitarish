using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Owner;
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
using Vitarish.Entities.Server.Extensions;
using Vitarish.Knowledge.Infrastructure.Handlers;
using Vitarish.Knowledge.Application.Services.RetrievalPlanning;

namespace Vitarish.Knowledge.Module;

public sealed class KnowledgeModule : Vitarish.Modules.Base.Module, IKnowledgeModule
{
    public override string Name => "Knowledge";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IKnowledgeModule, KnowledgeModule>();
        _ = builder.Services.AddTransientMultiTenantDbContext<KnowledgeDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddScoped<IKnowledgeChunker, HeadingAwareKnowledgeChunker>();
        _ = builder.Services.AddScoped<IKnowledgeImporter, KnowledgeImporter>();
        _ = builder.Services.AddScoped<IKnowledgeImportFileReader, KnowledgeImportFileReader>();
        _ = builder.Services.AddScoped<IKnowledgeIndexer, SearchKnowledgeIndexer>();
        _ = builder.Services.AddScoped<ISearchIndexProvider, KnowledgeSearchIndexProvider>();
        _ = builder.Services.AddScoped<IKnowledgeRetrievalPlanner, KnowledgeRetrievalPlanner>();
        _ = builder.Services.AddScoped<IKnowledgeRetriever, SearchKnowledgeRetriever>();
        _ = builder.Services.AddScoped<IKnowledgeContextBuilder, KnowledgeContextBuilder>();
        _ = builder.Services.AddScoped<IKnowledgeAnswerer, IntelligenceKnowledgeAnswerer>();
        _ = builder.Services.AddScoped<IKnowledgeEvaluator, KnowledgeEvaluator>();
        _ = builder.Services.AddScoped<IKnowledgeCorpusRepository, KnowledgeCorpusRepository>();
        _ = builder.Services.AddScoped<IKnowledgeSourceRepository, KnowledgeSourceRepository>();
        _ = builder.Services.AddScoped<IKnowledgeChunkRepository, KnowledgeChunkRepository>();
        _ = builder.Services.AddScoped<IKnowledgeQuestionRepository, KnowledgeQuestionRepository>();
        _ = builder.Services.AddScoped<IKnowledgeAskTraceRepository, KnowledgeAskTraceRepository>();
        _ = builder.Services.AddScoped<IKnowledgeIndexRunRepository, KnowledgeIndexRunRepository>();
        _ = builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
        _ = builder.Services.AddScoped<IKnowledgeDiagnosticsService, KnowledgeDiagnosticsService>();
        _ = builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(KnowledgeDbContext).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(KnowledgeOwnerReadModelQuery).Assembly);
        });
        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
      await  app.MigrateDatabaseAsync<KnowledgeDbContext>();
    }

}
