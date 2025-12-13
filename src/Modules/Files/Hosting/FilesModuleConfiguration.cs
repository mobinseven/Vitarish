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

namespace Vitarish.Files.Hosting;

internal static class FilesModuleConfiguration
{
    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {

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

        _ = services.AddSignalR().AddMessagePackProtocol();
    }

    public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("FilesModule requires a WebApplication for endpoint configuration.");
        }

        _ = app.UseMiddleware<FileLoadCors>();
        _ = app.UseMiddleware<ObjectStorageMiddleware>();
        _ = app.MapHub<FilesHub>("/files/hubs/transfer");
    }

    public static async Task ConfigureEndpointsAsync(IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("FilesModule requires a WebApplication for endpoint configuration.");
        }
        using IServiceScope scope = app.Services.CreateScope();
        IObjectStorageService storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await storage.EnsureBucketAsync(app.Lifetime.ApplicationStopping);
    }
}
