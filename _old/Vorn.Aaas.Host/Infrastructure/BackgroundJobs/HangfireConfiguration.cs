using Hangfire;
using Hangfire.SqlServer;
using Vorn.Aaas.Interface.Infrastructure.Services;

namespace Vorn.Aaas.Host.Infrastructure.BackgroundJobs;

/// <summary>
/// Extension methods for configuring Hangfire in the Host project.
/// </summary>
public static class HangfireConfiguration
{
    /// <summary>
    /// Adds Hangfire services and configures background job processing.
    /// </summary>
    public static void AddHangfireBackgroundJobs(this WebApplicationBuilder builder)
    {
        string? connectionString = builder.Configuration.GetConnectionString("AaasDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'AaasDb' is not configured. Hangfire requires a valid SQL Server connection string.");

        // Configure Hangfire to use SQL Server storage
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(1),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "Hangfire"
            }));

        // Add the Hangfire server for background job processing
        builder.Services.AddHangfireServer(options =>
        {
            // Make worker count configurable via configuration (e.g., "Hangfire:WorkerCount" in appsettings.json), default to Environment.ProcessorCount
            int workerCount = builder.Configuration.GetValue<int?>("Hangfire:WorkerCount") ?? Environment.ProcessorCount;
            if (workerCount <= 0)
                throw new InvalidOperationException($"Hangfire:WorkerCount must be positive. Configured value: {workerCount}");
            options.WorkerCount = workerCount;
            options.ServerName = $"{Environment.MachineName}:{Guid.NewGuid()}";
        });

        // Register message queue and processor
        builder.Services.AddTransient<IMessageQueue, HangfireMessageQueue>();
        builder.Services.AddTransient<MessageProcessor>();
        
        // Register stub IMessageClient until Vorn.Caas dependency is available
        builder.Services.AddTransient<Vorn.Caas.Client.Abstractions.IMessageClient, Vorn.Caas.Client.Abstractions.MessageClientStub>();
    }

    /// <summary>
    /// Maps Hangfire dashboard endpoint if enabled via configuration.
    /// </summary>
    public static void MapHangfireDashboard(this WebApplication app)
    {
        bool enableDashboard = app.Configuration.GetValue<bool>("Hangfire:EnableDashboard");
        
        if (enableDashboard)
        {
            app.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "Vorn.Aaas Background Jobs"
            });
        }
    }
}
