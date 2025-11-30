using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.DependencyInjection;
using Vorn.Ai.Client.Abstractions;
using Vorn.Ai.Client.Internal.Services;
public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddAiClient(this WebApplicationBuilder builder)
    {
        if(!builder.Services.Any(x => x.ServiceType == typeof(IApiClientService)))
        {
            throw new Exception("The IApiClientService is required.");
        }
        _ = builder.Services.AddScoped<IResponderClient, ResponderClient>();
        _ = builder.Services.AddScoped<ITranscriberClient, TranscriberClient>();
        _ = builder.Services.AddScoped<IStreamResponder, StreamResponder>();
        _ = builder.Services.AddScoped<IStreamRewriter, StreamRewriter>();
        _ = builder.Services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        _ = builder.Services.AddScoped<IModelRegistry, ModelRegistry>();
        _ = builder.Services.AddSignalRClient<TranscriberClientDefinition>();
        _ = builder.Services.AddSignalRClient<ResponderClientDefinition>();
        return builder;
    }
}
