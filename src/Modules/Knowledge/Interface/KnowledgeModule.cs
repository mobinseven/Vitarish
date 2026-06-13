using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Repositories;
using Vitarish.Modules.Base;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Knowledge.Infrastructure.Services.Answering;
using Vitarish.Knowledge.Infrastructure.Services.Evaluation;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Knowledge.Infrastructure.Services;
using Vitarish.Knowledge.Infrastructure.Services.Indexing;
using Vitarish.Knowledge.Infrastructure.Services.Importing;
using Vitarish.Knowledge.Infrastructure.Services.Chunking;

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
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
