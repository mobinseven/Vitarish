namespace Vitarish.Main.DependencyInjection;

public static class HostRedisExtensions
{
    public static void AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetRedisConnectionString();
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
            _ => StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString));
        services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);
        Vitarish.Modules.Base.Infrastructure.Idempotency.OperationReceiptRegistrationExtensions
            .AddRedisOperationReceipts(services);
    }

    public static void AddRedisCaching(this WebApplicationBuilder builder) =>
        builder.Services.AddRedisCaching(builder.Configuration);

    public static string GetRedisConnectionString(this IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Redis");
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("ConnectionStrings:Redis not supplied")
            : connectionString;
    }
}
