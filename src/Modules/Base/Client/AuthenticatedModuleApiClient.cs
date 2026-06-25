using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vitarish.Modules.Base.Client;

public abstract class AuthenticatedModuleApiClient(HttpClient httpClient)
{
    protected Task<ModuleApiClientResult<TResponse>> GetJsonAsync<TResponse>(string requestUri, CancellationToken cancellationToken) =>
        SendJsonAsync<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);

    protected Task<ModuleApiClientResult<TResponse>> PostJsonAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken) =>
        SendJsonAsync<TResponse>(CreateJsonRequest(HttpMethod.Post, requestUri, request), cancellationToken);

    protected Task<ModuleApiClientResult<TResponse>> PutJsonAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken) =>
        SendJsonAsync<TResponse>(CreateJsonRequest(HttpMethod.Put, requestUri, request), cancellationToken);

    protected Task<ModuleApiClientResult<TResponse>> PatchJsonAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken) =>
        SendJsonAsync<TResponse>(CreateJsonRequest(HttpMethod.Patch, requestUri, request), cancellationToken);

    protected Task<ModuleApiClientResult<TResponse>> DeleteJsonAsync<TResponse>(string requestUri, CancellationToken cancellationToken) =>
        SendJsonAsync<TResponse>(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);

    protected async Task<ModuleApiClientResult<HttpResponseMessage>> SendRawAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            return ModuleApiClientResult<HttpResponseMessage>.Success(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ModuleApiClientResult<HttpResponseMessage>.Failed(ModuleApiClientFailure.Canceled());
        }
        catch (OperationCanceledException)
        {
            return ModuleApiClientResult<HttpResponseMessage>.Failed(ModuleApiClientFailure.Timeout());
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or IOException)
        {
            return ModuleApiClientResult<HttpResponseMessage>.Failed(ModuleApiClientFailure.Transport(ex.Message));
        }
    }

    protected abstract ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue);

    protected virtual async Task<ModuleApiClientFailure> CreateFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string message = "Module API request failed.";
        try
        {
            string? content = response.Content is null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(content)) message = content;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException) { }
        return CreateFailure(response.StatusCode, message, (int)response.StatusCode);
    }

    private async Task<ModuleApiClientResult<TResponse>> SendJsonAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ModuleApiClientResult<HttpResponseMessage> sent = await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
        if (!sent.IsSuccess) return ModuleApiClientResult<TResponse>.Failed(sent.Failure!);
        using HttpResponseMessage response = sent.Value!;
        if (!response.IsSuccessStatusCode) return ModuleApiClientResult<TResponse>.Failed(await CreateFailureAsync(response, cancellationToken).ConfigureAwait(false));
        try
        {
            TResponse? value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken).ConfigureAwait(false);
            return value is null ? ModuleApiClientResult<TResponse>.Failed(CreateFailure(HttpStatusCode.NoContent, "Module API returned an empty response.", (int)response.StatusCode)) : ModuleApiClientResult<TResponse>.Success(value);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return ModuleApiClientResult<TResponse>.Failed(ModuleApiClientFailure.Deserialization(ex.Message, (int)response.StatusCode));
        }
    }

    private static HttpRequestMessage CreateJsonRequest<TRequest>(HttpMethod method, string requestUri, TRequest request) =>
        new(method, requestUri) { Content = JsonContent.Create(request) };
}

public sealed record ModuleApiClientFailure(string Code, string Message, int? StatusCode = null)
{
    public static ModuleApiClientFailure Canceled() => new("Canceled", "Module API request was canceled.");
    public static ModuleApiClientFailure Timeout() => new("Timeout", "Module API request timed out.");
    public static ModuleApiClientFailure Transport(string message) => new("Transport", message);
    public static ModuleApiClientFailure Deserialization(string message, int? statusCode) => new("Deserialization", message, statusCode);
}

public sealed record ModuleApiClientResult<T>(T? Value, ModuleApiClientFailure? Failure)
{
    public bool IsSuccess => Failure is null;
    public static ModuleApiClientResult<T> Success(T value) => new(value, null);
    public static ModuleApiClientResult<T> Failed(ModuleApiClientFailure failure) => new(default, failure);
}
