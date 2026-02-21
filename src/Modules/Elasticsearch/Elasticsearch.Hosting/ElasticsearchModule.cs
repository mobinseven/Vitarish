using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vitarish.Modules.Base;
using Vitarish.Modules.Elasticsearch.Infrastructure.DependencyInjection;

namespace Vitarish.Modules.Elasticsearch.Hosting;

public sealed class ElasticsearchModule : Module
{
    public override string Name => "Elasticsearch";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        IConfigurationSection section = builder.Configuration.GetSection("Elasticsearch");

        if (!section.Exists())
        {
            IsAvailable = false;
            return Task.CompletedTask;
        }

        _ = builder.Services.AddElasticsearchInfrastructure(builder.Configuration);
        IsAvailable = true;
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
