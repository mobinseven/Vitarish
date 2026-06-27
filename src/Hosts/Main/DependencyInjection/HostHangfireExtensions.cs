using Hangfire;
using Hangfire.PostgreSql;
using Vitarish.Main.Authorization;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.Platform;
using Vitarish.Modules.Base.Core.Platform.Transactions;

namespace Vitarish.Main.DependencyInjection;

/// <summary>
/// Extension methods for configuring Hangfire in the Host project.
/// </summary>
public static class HostHangfireExtensions
{
    /// <summary>
    /// Adds Hangfire services and configures background job processing.
    /// </summary>
    public static void AddHostHangfire(this WebApplicationBuilder builder, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured. Hangfire requires a valid PostgreSQL connection string.");
        }

        // Configure Hangfire to use PostgreSQL storage
        _ = builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                InvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(1),
                SchemaName = "hangfire"
            }));

        // Add the Hangfire server for background job processing
        _ = builder.Services.AddHangfireServer(options =>
        {
            // Make worker count configurable via configuration (e.g., "Hangfire:WorkerCount" in appsettings.json), default to Environment.ProcessorCount
            int workerCount = builder.Configuration.GetValue<int?>("Hangfire:WorkerCount") ?? Environment.ProcessorCount;
            if (workerCount <= 0)
            {
                throw new InvalidOperationException($"Hangfire:WorkerCount must be positive. Configured value: {workerCount}");
            }

            options.WorkerCount = workerCount;
            options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid()}";
        });

        // Centralized platform services
        _ = builder.Services.AddSingleton<IExternalApiTransactionLogger, LoggingApiTransactionLogger>();
    }

    /// <summary>
    /// Maps Hangfire dashboard endpoint if enabled via configuration.
    /// </summary>
    public static void MapHangfireDashboard(this WebApplication app)
    {
        bool enableDashboard = app.Configuration.GetValue<bool>("Hangfire:EnableDashboard");

        if (enableDashboard)
        {
            _ = app.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
                DashboardTitle = "Vitarish Background Jobs"
            });
        }
    }
}
