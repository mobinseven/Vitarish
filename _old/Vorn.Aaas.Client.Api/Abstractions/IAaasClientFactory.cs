namespace Vorn.Aaas.Client.Api.Abstractions;

public interface IAaasClientFactory
{
    Task<HttpClient> CreateClient(string? endpoint = null, string? clientId = null);
}
