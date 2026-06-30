using Microsoft.Extensions.DependencyInjection;
using Vitarish.Journey.Application;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Infrastructure.Consent;
using Vitarish.Journey.Infrastructure.Destination;
using Vitarish.Journey.Infrastructure.Profile;
using Vitarish.Journey.Infrastructure.ReturnUrl;
using Vitarish.Journey.Infrastructure.Workspace;

namespace Vitarish.Journey.Infrastructure.DependencyInjection;

public static class JourneyServiceCollectionExtensions
{
    public static IServiceCollection AddJourney(this IServiceCollection services)
    {
        services.AddScoped<IJourneyResolver, JourneyResolver>();
        services.AddScoped<IJourneyProfileReader, JourneyProfileReader>();
        services.AddScoped<IJourneyConsentReader, JourneyConsentReader>();
        services.AddScoped<IJourneyWorkspaceReader, JourneyWorkspaceReader>();
        services.AddScoped<IJourneyReturnUrlPolicy, JourneyReturnUrlPolicy>();
        services.AddScoped<IJourneyDestinationResolver, JourneyDestinationResolver>();

        return services;
    }
}
