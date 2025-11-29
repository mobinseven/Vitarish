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
        if (!builder.Services.Any(x => x.ServiceType == typeof(IApiClientService)))
        {
            throw new Exception("The IApiClientService is required.");
        }
        builder.Services.AddScoped<IResponseHubClient, ResponseHubClient>();
        builder.Services.AddScoped<ISpeechHubClient, SpeechHubClient>();
        builder.Services.AddScoped<IStreamResponder, StreamResponder>();
        builder.Services.AddScoped<IStreamRewriter, StreamRewriter>();
        builder.Services.AddScoped<IStreamTranscriber, StreamTranscriber>();
        builder.Services.AddSignalRClient<AiSpeechHub>();
        builder.Services.AddSignalRClient<AiResponseHub>();
        return builder;
    }
}
