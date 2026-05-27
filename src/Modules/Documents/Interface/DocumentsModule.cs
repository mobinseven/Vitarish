using Gotenberg.Sharp.API.Client.Domain.Settings;
using Gotenberg.Sharp.API.Client.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Documents.Application.Abstractions;
using Vitarish.Documents.Domain.Options;
using Vitarish.Modules.Base;

namespace Vitarish.Documents.Interface;

public class DocumentsModule : Module, IDocumentsModule
{
    public override string Name => "Documents";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        IServiceCollection services = builder.Services;

        _ = services.AddSingleton<IDocumentsModule, DocumentsModule>();

        _ = services.AddOptions<GotenbergSharpClientOptions>()
            .Configure<IOptions<GotenbergOptions>>((clientOptions, gotenbergOptions) =>
            {
                GotenbergOptions opts = gotenbergOptions.Value;
                if (!string.IsNullOrEmpty(opts.ServiceUrl))
                {
                    clientOptions.ServiceUrl = new Uri(opts.ServiceUrl);
                }
                if (opts.TimeoutSeconds > 0)
                {
                    clientOptions.TimeOut = TimeSpan.FromSeconds(opts.TimeoutSeconds);
                }
            });

        _ = services.AddGotenbergSharpClient();
        _ = services.AddVitarishDocumentsInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
