using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Vitarish.TaminInsurance.Client;

public sealed class TaminInsuranceClient : ITaminInsuranceClient
{
    private const string ApiKeyHeader = "X-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly TaminInsuranceClientOptions _options;

    public TaminInsuranceClient(HttpClient httpClient, TaminInsuranceClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;

        if (string.IsNullOrWhiteSpace(_options.EffectiveBaseUrl))
            throw new InvalidOperationException("Tamin base URL is required.");
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Tamin API key is required.");

        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri(_options.EffectiveBaseUrl, UriKind.Absolute);
    }

    public Task<TaminAuthResponse?> AuthenticateAsync(TaminAuthRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<TaminAuthResponse>(HttpMethod.Post, "api/auth/login", request, bearerToken: null, idempotencyKey: null, cancellationToken);
    }

    public Task<TaminEntitlementResponse?> CheckEntitlementAsync(string bearerToken, TaminEntitlementRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<TaminEntitlementResponse>(HttpMethod.Post, "api/prescription/entitlement", request, bearerToken, idempotencyKey: null, cancellationToken);
    }

    public Task<TaminSubmissionResponse?> SubmitPrescriptionAsync(string bearerToken, TaminPrescriptionSubmitRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<TaminSubmissionResponse>(HttpMethod.Post, "api/prescription/submit", request, bearerToken, idempotencyKey, cancellationToken);
    }

    public Task<TaminSubmissionResponse?> EditPrescriptionAsync(string bearerToken, string remotePrescriptionId, TaminPrescriptionSubmitRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePrescriptionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<TaminSubmissionResponse>(HttpMethod.Put, $"api/prescription/{Uri.EscapeDataString(remotePrescriptionId)}", request, bearerToken, idempotencyKey, cancellationToken);
    }

    public Task<TaminFetchResponse?> FetchPrescriptionAsync(string bearerToken, string remotePrescriptionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePrescriptionId);
        return SendAsync<TaminFetchResponse>(HttpMethod.Get, $"api/prescription/{Uri.EscapeDataString(remotePrescriptionId)}", content: null, bearerToken, idempotencyKey: null, cancellationToken);
    }

    public async Task CancelPrescriptionAsync(string bearerToken, string remotePrescriptionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(remotePrescriptionId);
        _ = await SendAsync<object>(HttpMethod.Delete, $"api/prescription/{Uri.EscapeDataString(remotePrescriptionId)}", content: null, bearerToken, idempotencyKey: null, cancellationToken);
    }

    public Task<TaminReferenceDataResponse?> FetchReferenceDataAsync(string codeSystem, string? sinceVersion = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeSystem);
        var url = $"api/reference/{Uri.EscapeDataString(codeSystem)}";
        if (!string.IsNullOrWhiteSpace(sinceVersion))
            url += $"?sinceVersion={Uri.EscapeDataString(sinceVersion)}";

        return SendAsync<TaminReferenceDataResponse>(HttpMethod.Get, url, content: null, bearerToken: null, idempotencyKey: null, cancellationToken);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? content, string? bearerToken, string? idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);
        request.Headers.Add(ApiKeyHeader, _options.ApiKey);
        if (content is not null)
            request.Content = JsonContent.Create(content);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TaminInsuranceClientException(response.StatusCode, responseBody);
        }

        if (response.Content is null || response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default;

        var successBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(successBody))
            return default;

        return JsonSerializer.Deserialize<T>(successBody, JsonOptions);
    }
}
