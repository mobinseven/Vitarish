using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Caas.Client.Abstractions;
using Vorn.Caas.Client.Internal;
public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddCaasClient(this IServiceCollection services)
    {

        services.AddScoped<IMessageClient, MessageClient>();

        return services;
    }

    public static WebApplicationBuilder AddCaasClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddCaasClient();
        return builder;
    }
}
