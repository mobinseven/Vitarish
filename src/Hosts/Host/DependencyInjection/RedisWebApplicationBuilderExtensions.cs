using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Host.DependencyInjection;

public static class RedisWebApplicationBuilderExtensions
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
