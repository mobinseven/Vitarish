using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Api;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Files.Infrastructure.DependencyInjection;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Files.Interface.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Files.Module;

public class FilesModule : Vitarish.Modules.Base.Module, IFilesModule
{
    public override string Name => "Files";
    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        IServiceCollection services = builder.Services;
        ConfigurationManager configuration = builder.Configuration;
        _ = services.AddSingleton<IFilesModule, FilesModule>();
        _ = services.AddSingleton<IAmazonS3>(sp =>
        {
            MinioOptions minioOptions = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            AmazonS3Config amazonS3Config = new()
            {
                ServiceURL = minioOptions.BuildServiceUrl(),
                ForcePathStyle = minioOptions.ForcePathStyle,
                UseHttp = !minioOptions.UseSsl
            };
            if (!string.IsNullOrWhiteSpace(minioOptions.Region))
            {
                amazonS3Config.AuthenticationRegion = minioOptions.Region;
            }
            BasicAWSCredentials credentials = new(minioOptions.AccessKey, minioOptions.SecretKey);
            return new AmazonS3Client(credentials, amazonS3Config);
        });

        _ = services.AddSingleton<IObjectStorageService, AwsS3ObjectStorageService>();
        _ = services.AddScoped<IFilesService, FilesService>();
        _ = services.AddScoped<IFileInformationService, FileInformationService>();

        if (configureDbContext != null)
        {
            _ = services.AddTransientMultiTenantDbContext<FilesDbContext>((_, options) =>
            {
                configureDbContext(options);
                options.UseNpgsql(
                builder.Configuration.GetSection("ConnectionStrings")["Db"],
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(FilesDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure();
                });
            });
        }

        services.AddVitarishFilesDomainEntityHandlers();
        _ = services.AddVitarishFilesInfrastructure();
        _ = services.AddFilesBackgroundJobs();

        _ = services.ConfigureFilesInterface();
    }
    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<FilesDbContext>();
        _ = app.MapFilesApi();
    }
}
