
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Minio;
using Vorn.Aaas.Client.Api.HubFilter;
using Vorn.Files.Core.Infrastructure.Data;
using Vorn.Files.Core.Infrastructure.Services;
using Vorn.Files.Core.Presentation.Interfaces;
using Vorn.Files.Core.Presentation.WebApplication;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Interfaces;
using Vorn.Files.Host.Infrastructure.Services;
using Vorn.Files.Host.Presentation.Hub;

public static class WebApplicationExtension
{
    public static async Task<WebApplicationBuilder> AddFilesHost(this WebApplicationBuilder builder)
    {
        VornOptions vornOptions = new();
        IConfigurationSection section = builder.Configuration.GetSection(VornOptions.Section);
        section.Bind(vornOptions);
        _ = builder.Services.AddSingleton(vornOptions);

        MinioOptions minioOptions = new(builder.Configuration);
        _ = builder.Services.AddSingleton(minioOptions);

        _ = builder.Services.AddSingleton<IMinioClient>(_ =>
        {
            IMinioClient clientBuilder = new MinioClient()
                .WithEndpoint(minioOptions.Endpoint, minioOptions.Port)
                .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey);

            if(minioOptions.UseSsl)
            {
                clientBuilder = clientBuilder.WithSSL();
            }

            return clientBuilder.Build();
        });
        _ = builder.Services.AddSingleton<IObjectStorageService, MinioObjectStorageService>();
        _ = builder.Services.AddHttpClient(nameof(ObjectStorageFileMiddleware));
        _ = builder.Services.AddHostedService<ObjectStorageStartupSelfTest>();
        _ = builder.Services.AddSingleton<FfmpegBinaryService>();
        _ = builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
        _ = builder.Services.AddHostedService(sp => (ThumbnailService)sp.GetRequiredService<IThumbnailService>());
        _ = builder.Services.AddSingleton<IWebpConversionService, WebpConversionService>();
        _ = builder.Services.AddHostedService(sp => (WebpConversionService)sp.GetRequiredService<IWebpConversionService>());
        _ = builder.Services.AddScoped<IFilesService, FilesService>();
        _ = builder.Services.AddScoped<IFileInformationService, FileInformationService>();
        _ = builder.Services
            .AddMultiTenant<TenantInfo>()
            .WithEchoStore()
            .WithDelegateStrategy(async context =>
            {
                return context is HttpContext httpContext
                    ? await MultitenantHelpers.ResolveTenantAsync(httpContext)
                    : throw new System.Exception("context is not HttpContext");
            });
        _ = builder.Services.AddControllers();
        void configureSqlServer(DbContextOptionsBuilder options)
        {
            _ = options.UseSqlServer(
            builder.Configuration.GetConnectionString("SqlDb"),
            b =>
            {
                _ = b.MigrationsAssembly("Vorn.Files.Host");
                _ = b.EnableRetryOnFailure();
            });
        }

        _ = builder.AddCore(configureSqlServer);
        await builder.Services.ConfigureAaasClient(builder.Configuration);
        _ = builder.Services.AddSignalR(o => o.AddFilter<RequestIdentityContextHubFilter>()).AddMessagePackProtocol();
        _ = builder.AddDashboardServices();
        return builder;
    }

    public static async Task<WebApplication> UseFilesHost(this WebApplication app)
    {
        app.UseDashboardServices();
        _ = await app.MigrateDatabaseAsync<FilesDbContext>();
        using IServiceScope scope = app.Services.CreateScope();
        IObjectStorageService storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await storage.EnsureBucketAsync(app.Lifetime.ApplicationStopping);
        _ = app.UseMultiTenant();
        _ = app.UseMiddleware<FileLoadCors>();
        _ = app.UseMiddleware<ThumbnailProvider>();
        _ = app.UseMiddleware<ObjectStorageFileMiddleware>();
        _ = app.MapHub<FilesHub>("/files/hubs/transfer").RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
        });
        return app;
    }
}
