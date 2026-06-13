using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Knowledge.Application;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Answering;
using Vitarish.Knowledge.Application.Chunking;
using Vitarish.Knowledge.Application.Evaluation;
using Vitarish.Knowledge.Application.Indexing;
using Vitarish.Knowledge.Application.Importing;
using Vitarish.Knowledge.Application.Retrieval;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Repositories;
using Vitarish.Modules.Base;
using Vitarish.Search.Domain.Abstractions;

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
        _ = builder.Services.AddScoped<IKnowledgeRepository, EfKnowledgeRepository>();
        _ = builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
