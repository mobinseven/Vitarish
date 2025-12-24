namespace Vitarish.Main.DependencyInjection;

public static class HostRedisExtensions
{
    public static void AddRedisCaching(this IServiceCollection services, IConfiguration configuration) =>
        services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetRedisConnectionString());

    public static void AddRedisCaching(this WebApplicationBuilder builder) =>
        builder.Services.AddStackExchangeRedisCache(options => options.Configuration = builder.Configuration.GetRedisConnectionString());

    public static string GetRedisConnectionString(this IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Redis");
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("ConnectionStrings:Redis not supplied")
            : connectionString;
    }
}
