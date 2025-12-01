using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Api.HealthChecks;
using Vitarish.Communications.Api.Messages.Endpoints;
using Vitarish.Communications.Infrastructure.Messages.DependencyInjection;
using Vitarish.Communications.Infrastructure.Messages.Options;

namespace Vitarish.Communications.Api;

public static class CommunicationsApiExtensions
{
    public static IServiceCollection AddCommunicationsApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommunicationsMessaging(configuration);

        CommunicationsOptions? communicationsOptions = configuration
            .GetSection(CommunicationsOptions.Section)
            .Get<CommunicationsOptions>();

        if (string.IsNullOrWhiteSpace(communicationsOptions?.IdentityAuthority))
        {
            throw new InvalidOperationException(
                "IdentityAuthority must be configured in Communications:IdentityAuthority configuration section");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = communicationsOptions.IdentityAuthority;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidAudience = "communications-api";
                options.TokenValidationParameters.ValidTypes = ["at+jwt"];
            });

        services.AddAuthorization();

        return services;
    }

    public static void MapCommunicationsApi(this WebApplication app)
    {
        app.MapMessagesEndpoints();

        app.MapGet("/", () => "Communications.Api is running.")
            .WithName("HealthCheck");

        app.UseStartupSmsTest();
    }
}
