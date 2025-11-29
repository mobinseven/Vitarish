
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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
    public static WebApplicationBuilder AddFilesHost(this WebApplicationBuilder builder)
    {
        VornOptions vornOptions = new();
        IConfigurationSection section = builder.Configuration.GetSection(VornOptions.Section);
        section.Bind(vornOptions);
        _ = builder.Services.AddSingleton(vornOptions);
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
        _ = builder.AddCore(options =>
        {
            string exeFullName = Assembly.GetEntryAssembly()!.Location;
            string dirFullName = Path.GetDirectoryName(exeFullName)!;
            string dbPath = Path.Combine(dirFullName, $"{Assembly.GetEntryAssembly()!.GetName().Name}.db");
            _ = options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
        });
        _ = builder.Services.ConfigureAaasClient(builder.Configuration);
        _ = builder.Services.AddSignalR(o => o.AddFilter<RequestIdentityContextHubFilter>()).AddMessagePackProtocol();
        _ = builder.AddDashboardServices();
        return builder;
    }

    public static async Task<WebApplication> UseFilesHost(this WebApplication app)
    {
        app.UseDashboardServices();
        _ = await app.MigrateDatabaseAsync<FilesDbContext>();
        _ = app.UseMultiTenant();
        _ = app.UseMiddleware<FileLoadCors>();
        _ = app.UseMiddleware<ThumbnailProvider>();
        _ = app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = new DynamicContentTypeProvider() });
        _ = app.MapHub<FilesHub>("/files/hubs/transfer").RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
        });
        return app;
    }
}
