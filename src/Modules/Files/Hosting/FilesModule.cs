using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Hosting.Hubs;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Modules.Base;

namespace Vitarish.Files.Hosting;

/// <summary>
/// Module wiring for the Files stack using the shared module contracts.
/// </summary>
public class FilesModule : ModuleBase
{
    private readonly Action<DbContextOptionsBuilder>? configureDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesModule"/> class.
    /// </summary>
    public FilesModule()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesModule"/> class with database configuration.
    /// </summary>
    /// <param name="configureDbContext">Delegate to configure the database context.</param>
    public FilesModule(Action<DbContextOptionsBuilder> configureDbContext)
    {
        this.configureDbContext = configureDbContext;
    }

    /// <inheritdoc />
    public override string Name => "Files";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        Action<DbContextOptionsBuilder>? dbConfigurator = (configureDbContext ?? this.configureDbContext) ?? throw new InvalidOperationException("FilesModule requires a DbContext configuration delegate.");

        _ = services.AddSingleton<IMinioClient>(_ =>
        {
            MinioOptions minioOptions = _.GetRequiredService<IOptions<MinioOptions>>().Value;
            IMinioClient clientBuilder = new MinioClient()
                .WithEndpoint(minioOptions.Endpoint, minioOptions.Port)
                .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey);

            if (minioOptions.UseSsl)
            {
                clientBuilder = clientBuilder.WithSSL();
            }

            return clientBuilder.Build();
        });

        _ = services.AddSingleton<IObjectStorageService, MinioObjectStorageService>();
        _ = services.AddHttpClient(nameof(ObjectStorageMiddleware))
            .ConfigureHttpClient((sp, client) =>
            {
                MinioOptions minioOptions = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(minioOptions.HttpRequestTimeoutSeconds);
            });
        _ = services.AddScoped<IFilesService, FilesService>();
        _ = services.AddScoped<IFileInformationService, FileInformationService>();
        _ = services.AddControllers();

        if (configureDbContext != null)
        {
            _ = services.AddDbContext<FilesDbContext>((sp, options) =>
            {
                configureDbContext(options);
            }, ServiceLifetime.Transient, ServiceLifetime.Transient);

            _ = services.AddDbContextPool<FilesDbContext>((_, options) =>
            {
                configureDbContext(options);
            });
        }

        services.AddVitarishFilesDomainEntityHandlers();
        _ = services.AddVitarishFilesInfrastructure();
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("FilesModule requires a WebApplication for endpoint configuration.");
        }

        //_ = app.UseMiddleware<FileLoadCors>(); TODO: remporary disabled
        _ = app.UseMiddleware<ObjectStorageMiddleware>();
        _ = app.MapHub<FilesHub>("/files/hubs/transfer");
    }

    /// <inheritdoc />
    protected override async Task ConfigureEndpointsAsync(IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("FilesModule requires a WebApplication for endpoint configuration.");
        }
        using IServiceScope scope = app.Services.CreateScope();
        IObjectStorageService storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await storage.EnsureBucketAsync(app.Lifetime.ApplicationStopping);
    }

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) =>
        // Reserved for Files-specific health checks (e.g., object storage availability).
        builder.Services.AddHealthChecks();

    /// <inheritdoc />
    protected override void ConfigureSelfTests(WebApplicationBuilder builder) => builder.Services.AddHostedService<ObjectStorageStartupSelfTest>();
}
